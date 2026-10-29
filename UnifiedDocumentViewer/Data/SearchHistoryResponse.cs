namespace UnifiedDocumentViewer.Data;

public sealed record SearchHistoryResponse(
    long Id,
    string Vin,
    DateTime SearchedAtUtc,
    string Status,
    int DocumentsReturned,
    string? ErrorMessage);
