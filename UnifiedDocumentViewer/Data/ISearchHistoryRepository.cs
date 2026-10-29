namespace UnifiedDocumentViewer.Data;

public interface ISearchHistoryRepository
{
    Task AddAsync(
        SearchAttempt searchAttempt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchAttempt>> GetAsync(
        string? vin = null,
        CancellationToken cancellationToken = default);
}
