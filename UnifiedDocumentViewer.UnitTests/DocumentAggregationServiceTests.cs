using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnifiedDocumentViewer.Clients;
using UnifiedDocumentViewer.Data;
using UnifiedDocumentViewer.Models;
using UnifiedDocumentViewer.Services;

namespace UnifiedDocumentViewer.UnitTests;

public sealed class DocumentAggregationServiceTests
{
    [Fact]
    public async Task SearchAsync_WhenBothSourcesReturnDocuments_CombinesAndPersistsCompletedSearch()
    {
        var salesDocument = CreateDocument("SALE-1", "Sales", "purchase-invoice.pdf");
        var serviceDocument = CreateDocument("SERVICE-1", "Service", "inspection-report.pdf");
        var salesClient = CreateClient("Sales", salesDocument);
        var serviceClient = CreateClient("Service", serviceDocument);
        var historyRepository = new Mock<ISearchHistoryRepository>();
        var sut = CreateService(historyRepository, salesClient, serviceClient);

        var result = await sut.SearchAsync(" vin-shared-001 ");

        Assert.Equal("VIN-SHARED-001", result.Vin);
        Assert.Equal(2, result.Documents.Count);
        Assert.True(result.HasSuccessfulSource);
        Assert.True(result.Sources["Sales"].Succeeded);
        Assert.True(result.Sources["Service"].Succeeded);
        historyRepository.Verify(repository => repository.AddAsync(
            It.Is<SearchAttempt>(attempt =>
                attempt.Vin == "VIN-SHARED-001" &&
                attempt.Status == SearchAttemptStatuses.Completed &&
                attempt.DocumentsReturned == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WhenBothSourcesReturnEmpty_ReturnsEmptyCompletedResult()
    {
        var historyRepository = new Mock<ISearchHistoryRepository>();
        var sut = CreateService(
            historyRepository,
            CreateClient("Sales"),
            CreateClient("Service"));

        var result = await sut.SearchAsync("VIN-EMPTY-001");

        Assert.Empty(result.Documents);
        Assert.True(result.HasSuccessfulSource);
        Assert.All(result.Sources.Values, source => Assert.True(source.Succeeded));
        historyRepository.Verify(repository => repository.AddAsync(
            It.Is<SearchAttempt>(attempt =>
                attempt.Status == SearchAttemptStatuses.Completed &&
                attempt.DocumentsReturned == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WhenOneSourceFails_ReturnsSuccessfulDocumentsAndPartialFailureStatus()
    {
        var serviceDocument = CreateDocument("SERVICE-1", "Service", "maintenance-record.pdf");
        var salesClient = CreateFailingClient("Sales", new HttpRequestException("Sales unavailable"));
        var serviceClient = CreateClient("Service", serviceDocument);
        var historyRepository = new Mock<ISearchHistoryRepository>();
        var sut = CreateService(historyRepository, salesClient, serviceClient);

        var result = await sut.SearchAsync("VIN-FAIL-SALES");

        var returnedDocument = Assert.Single(result.Documents);
        Assert.Equal("SERVICE-1", returnedDocument.Id);
        Assert.False(result.Sources["Sales"].Succeeded);
        Assert.True(result.Sources["Service"].Succeeded);
        Assert.Equal("The external system is unavailable.", result.Sources["Sales"].Error);
        historyRepository.Verify(repository => repository.AddAsync(
            It.Is<SearchAttempt>(attempt =>
                attempt.Status == SearchAttemptStatuses.PartialFailure &&
                attempt.DocumentsReturned == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WhenBothSourcesFail_ReturnsNoDocumentsAndFailedStatus()
    {
        var historyRepository = new Mock<ISearchHistoryRepository>();
        var sut = CreateService(
            historyRepository,
            CreateFailingClient("Sales", new HttpRequestException("Sales unavailable")),
            CreateFailingClient("Service", new HttpRequestException("Service unavailable")));

        var result = await sut.SearchAsync("VIN-FAIL-BOTH");

        Assert.Empty(result.Documents);
        Assert.False(result.HasSuccessfulSource);
        Assert.All(result.Sources.Values, source => Assert.False(source.Succeeded));
        historyRepository.Verify(repository => repository.AddAsync(
            It.Is<SearchAttempt>(attempt =>
                attempt.Status == SearchAttemptStatuses.Failed &&
                attempt.DocumentsReturned == 0 &&
                attempt.ErrorMessage!.Contains("Sales") &&
                attempt.ErrorMessage.Contains("Service")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WhenDocumentsHaveDuplicateSourceAndId_RemovesDuplicates()
    {
        var first = CreateDocument("DUPLICATE-1", "Incorrect", "first.pdf");
        var duplicate = CreateDocument("duplicate-1", "Incorrect", "second.pdf");
        var historyRepository = new Mock<ISearchHistoryRepository>();
        var sut = CreateService(
            historyRepository,
            CreateClient("Sales", first, duplicate),
            CreateClient("Service"));

        var result = await sut.SearchAsync("VIN-DUPLICATES");

        var returnedDocument = Assert.Single(result.Documents);
        Assert.Equal("Sales", returnedDocument.SourceSystem);
        Assert.Equal("DUPLICATE-1", returnedDocument.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("NO!")]
    [InlineData("AB")]
    [InlineData("THIS-VIN-IS-LONGER-THAN-17")]
    public async Task SearchAsync_WhenVinIsInvalid_ThrowsAndPersistsInvalidAttempt(string vin)
    {
        var salesClient = new Mock<IDocumentSourceClient>();
        var serviceClient = new Mock<IDocumentSourceClient>();
        var historyRepository = new Mock<ISearchHistoryRepository>();
        var sut = CreateService(historyRepository, salesClient, serviceClient);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.SearchAsync(vin));

        salesClient.Verify(client => client.GetDocumentsAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        serviceClient.Verify(client => client.GetDocumentsAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        historyRepository.Verify(repository => repository.AddAsync(
            It.Is<SearchAttempt>(attempt => attempt.Status == SearchAttemptStatuses.Invalid),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_AlwaysUsesClientSourceSystemForDocumentAttribution()
    {
        var documentWithIncorrectSource = CreateDocument("SALE-1", "Incorrect", "purchase-invoice.pdf");
        var historyRepository = new Mock<ISearchHistoryRepository>();
        var sut = CreateService(
            historyRepository,
            CreateClient("Sales", documentWithIncorrectSource),
            CreateClient("Service"));

        var result = await sut.SearchAsync("VIN-SOURCE-001");

        var returnedDocument = Assert.Single(result.Documents);
        Assert.Equal("Sales", returnedDocument.SourceSystem);
    }

    [Fact]
    public async Task SearchAsync_WhenCallerCancels_PropagatesCancellationAndDoesNotPersistResult()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var historyRepository = new Mock<ISearchHistoryRepository>();
        var sut = CreateService(
            historyRepository,
            CreateClient("Sales"),
            CreateClient("Service"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.SearchAsync("VIN-CANCEL-001", cancellationSource.Token));

        historyRepository.Verify(repository => repository.AddAsync(
            It.IsAny<SearchAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static DocumentAggregationService CreateService(
        Mock<ISearchHistoryRepository> historyRepository,
        params Mock<IDocumentSourceClient>[] clients)
    {
        return new DocumentAggregationService(
            clients.Select(client => client.Object),
            historyRepository.Object,
            NullLogger<DocumentAggregationService>.Instance);
    }

    private static Mock<IDocumentSourceClient> CreateClient(
        string sourceSystem,
        params Document[] documents)
    {
        var client = new Mock<IDocumentSourceClient>();
        client.SetupGet(source => source.SourceSystem).Returns(sourceSystem);
        client.Setup(source => source.GetDocumentsAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Document>)documents);
        return client;
    }

    private static Mock<IDocumentSourceClient> CreateFailingClient(
        string sourceSystem,
        Exception exception)
    {
        var client = new Mock<IDocumentSourceClient>();
        client.SetupGet(source => source.SourceSystem).Returns(sourceSystem);
        client.Setup(source => source.GetDocumentsAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        return client;
    }

    private static Document CreateDocument(string id, string sourceSystem, string fileName) =>
        new(
            id,
            "VIN-TEST-001",
            fileName,
            "Test Document",
            new DateTime(2025, 1, 1),
            $"https://example.test/{id}",
            sourceSystem);
}
