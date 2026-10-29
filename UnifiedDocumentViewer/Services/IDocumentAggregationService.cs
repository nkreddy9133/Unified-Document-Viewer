using UnifiedDocumentViewer.Models;

namespace UnifiedDocumentViewer.Services;

public interface IDocumentAggregationService
{
    Task<DocumentSearchResult> SearchAsync(
        string vin,
        CancellationToken cancellationToken = default);
}
