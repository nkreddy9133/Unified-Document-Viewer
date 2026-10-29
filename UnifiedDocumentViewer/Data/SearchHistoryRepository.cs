using Microsoft.EntityFrameworkCore;

namespace UnifiedDocumentViewer.Data;

public sealed class SearchHistoryRepository(DocumentDbContext dbContext) : ISearchHistoryRepository
{
    public async Task AddAsync(
        SearchAttempt searchAttempt,
        CancellationToken cancellationToken = default)
    {
        dbContext.SearchAttempts.Add(searchAttempt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchAttempt>> GetAsync(
        string? vin = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SearchAttempts
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(vin))
        {
            query = query.Where(attempt => attempt.Vin == vin);
        }

        return await query
            .OrderByDescending(attempt => attempt.SearchedAtUtc)
            .ThenByDescending(attempt => attempt.Id)
            .ToListAsync(cancellationToken);
    }
}
