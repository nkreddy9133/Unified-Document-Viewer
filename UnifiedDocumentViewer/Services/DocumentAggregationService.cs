using Microsoft.Extensions.Logging;
using UnifiedDocumentViewer.Clients;
using UnifiedDocumentViewer.Data;
using UnifiedDocumentViewer.Models;

namespace UnifiedDocumentViewer.Services;

public sealed class DocumentAggregationService(
    IEnumerable<IDocumentSourceClient> sourceClients,
    ISearchHistoryRepository searchHistoryRepository,
    ILogger<DocumentAggregationService> logger) : IDocumentAggregationService
{
    public async Task<DocumentSearchResult> SearchAsync(
        string vin,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var attemptedVin = NormalizeForHistory(vin);

        try
        {
            vin = ValidateVin(vin);
        }
        catch (ArgumentException exception)
        {
            await RecordSearchAttemptAsync(
                attemptedVin,
                SearchAttemptStatuses.Invalid,
                0,
                exception.Message,
                cancellationToken);
            throw;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var clients = sourceClients.ToArray();
        var requests = clients.Select(client => GetDocumentsSafelyAsync(client, vin, cancellationToken));
        var responses = await Task.WhenAll(requests);
        cancellationToken.ThrowIfCancellationRequested();

        var documents = responses
            .SelectMany(response => response.Documents)
            .GroupBy(
                document => $"{document.SourceSystem}\u001f{document.Id}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        var sources = responses.ToDictionary(
            response => response.SourceSystem,
            response => new DocumentSourceStatus(response.Error is null, response.Error));

        var status = responses.All(response => response.Error is null)
            ? SearchAttemptStatuses.Completed
            : responses.Any(response => response.Error is null)
                ? SearchAttemptStatuses.PartialFailure
                : SearchAttemptStatuses.Failed;

        var errorMessage = string.Join(
            "; ",
            responses
                .Where(response => response.Error is not null)
                .Select(response => $"{response.SourceSystem}: {response.Error}"));

        await RecordSearchAttemptAsync(
            vin,
            status,
            documents.Length,
            string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage,
            cancellationToken);

        return new DocumentSearchResult(vin, documents, sources, DateTime.UtcNow);
    }

    private async Task RecordSearchAttemptAsync(
        string vin,
        string status,
        int documentsReturned,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await searchHistoryRepository.AddAsync(
                new SearchAttempt
                {
                    Vin = vin,
                    SearchedAtUtc = DateTime.UtcNow,
                    Status = status,
                    DocumentsReturned = documentsReturned,
                    ErrorMessage = errorMessage
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to persist search history for VIN {Vin}.", vin);
            throw;
        }
    }

    private static string NormalizeForHistory(string vin) =>
        (vin ?? string.Empty).Trim().ToUpperInvariant();

    private string ValidateVin(string vin)
    {
        var normalizedVin = vin?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedVin) ||
            normalizedVin.Length is < 3 or > 17 ||
            normalizedVin.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
        {
            logger.LogWarning("Rejected invalid VIN search value: {Vin}.", vin);
            throw new ArgumentException(
                "VIN must be between 3 and 17 characters and contain only letters, numbers, or hyphens.",
                nameof(vin));
        }

        return normalizedVin;
    }

    private async Task<SourceResponse> GetDocumentsSafelyAsync(
        IDocumentSourceClient client,
        string vin,
        CancellationToken cancellationToken)
    {
        try
        {
            var documents = await client.GetDocumentsAsync(vin, cancellationToken);
            var sourceDocuments = documents
                .Select(document => document with { SourceSystem = client.SourceSystem })
                .ToArray();

            return new SourceResponse(client.SourceSystem, sourceDocuments, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            const string error = "The external system request timed out.";
            logger.LogWarning("{SourceSystem} request timed out for VIN {Vin}.", client.SourceSystem, vin);
            return new SourceResponse(client.SourceSystem, [], error);
        }
        catch (HttpRequestException exception)
        {
            const string error = "The external system is unavailable.";
            logger.LogWarning(exception, "{SourceSystem} is unavailable for VIN {Vin}.", client.SourceSystem, vin);
            return new SourceResponse(client.SourceSystem, [], error);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            const string error = "The external system returned an unexpected error.";
            logger.LogError(exception, "Unexpected error from {SourceSystem} for VIN {Vin}.", client.SourceSystem, vin);
            return new SourceResponse(client.SourceSystem, [], error);
        }
    }

    private sealed record SourceResponse(
        string SourceSystem,
        IReadOnlyList<Document> Documents,
        string? Error);
}
