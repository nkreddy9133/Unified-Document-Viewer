namespace UnifiedDocumentViewer.Models;

public sealed record Document(
    string Id,
    string Vin,
    string FileName,
    string DocumentType,
    DateTime DocumentDate,
    string? DownloadUrl,
    string SourceSystem);
