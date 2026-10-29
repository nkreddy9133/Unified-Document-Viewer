namespace UnifiedDocumentViewer.Models;

public sealed record DocumentSourceStatus(
    bool Succeeded,
    string? Error = null);

public sealed record DocumentSearchResult(
    string Vin,
    IReadOnlyList<Document> Documents,
    IReadOnlyDictionary<string, DocumentSourceStatus> Sources,
    DateTime RetrievedAt)
{
    public bool HasSuccessfulSource => Sources.Values.Any(source => source.Succeeded);
}
