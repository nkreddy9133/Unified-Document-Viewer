namespace UnifiedDocumentViewer.Data;

public static class SearchAttemptStatuses
{
    public const string Completed = "Completed";
    public const string PartialFailure = "PartialFailure";
    public const string Failed = "Failed";
    public const string Invalid = "Invalid";
}

public sealed class SearchAttempt
{
    public long Id { get; set; }

    public required string Vin { get; set; }

    public DateTime SearchedAtUtc { get; set; }

    public required string Status { get; set; }

    public int DocumentsReturned { get; set; }

    public string? ErrorMessage { get; set; }
}
