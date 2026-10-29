using System.Net.Http.Json;
using UnifiedDocumentViewer.Models;

namespace UnifiedDocumentViewer.Clients;

public sealed class SalesSystemClient(HttpClient httpClient) : IDocumentSourceClient
{
    public string SourceSystem => "Sales";

    public async Task<IReadOnlyList<Document>> GetDocumentsAsync(
        string vin,
        CancellationToken cancellationToken = default)
    {
        var documents = await httpClient.GetFromJsonAsync<List<Document>>(
            $"documents/{Uri.EscapeDataString(vin)}",
            cancellationToken);

        return documents ?? [];
    }
}
