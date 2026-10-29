using UnifiedDocumentViewer.Models;

namespace UnifiedDocumentViewer.Clients;

public interface IDocumentSourceClient
{
    string SourceSystem { get; }

    Task<IReadOnlyList<Document>> GetDocumentsAsync(
        string vin,
        CancellationToken cancellationToken = default);
}
