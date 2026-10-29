var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.MapGet("/sales/documents/{vin}", (string vin, bool fail = false) =>
    MockSystems.GetResponse("Sales", vin, fail));

app.MapGet("/service/documents/{vin}", (string vin, bool fail = false) =>
    MockSystems.GetResponse("Service", vin, fail));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

internal static class MockSystems
{
    private static readonly IReadOnlyDictionary<string, MockDocument[]> SalesDocuments =
        new Dictionary<string, MockDocument[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["VIN-SHARED-001"] =
            [
                new("SALE-001", "VIN-SHARED-001", "purchase-invoice.pdf", "Purchase Invoice", new DateTime(2025, 1, 15), "https://sales.example/documents/SALE-001", "Sales")
            ],
            ["VIN-SALES-001"] =
            [
                new("SALE-002", "VIN-SALES-001", "sales-agreement.pdf", "Sales Agreement", new DateTime(2025, 2, 20), "https://sales.example/documents/SALE-002", "Sales"),
                new("SALE-003", "VIN-SALES-001", "purchase-invoice.pdf", "Purchase Invoice", new DateTime(2025, 2, 20), "https://sales.example/documents/SALE-003", "Sales")
            ],
            ["VIN-FAIL-SERVICE"] =
            [
                new("SALE-004", "VIN-FAIL-SERVICE", "sales-agreement.pdf", "Sales Agreement", new DateTime(2025, 3, 5), "https://sales.example/documents/SALE-004", "Sales")
            ]
        };

    private static readonly IReadOnlyDictionary<string, MockDocument[]> ServiceDocuments =
        new Dictionary<string, MockDocument[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["VIN-SHARED-001"] =
            [
                new("SERVICE-001", "VIN-SHARED-001", "inspection-report.pdf", "Inspection Report", new DateTime(2025, 3, 10), "https://service.example/documents/SERVICE-001", "Service")
            ],
            ["VIN-SERVICE-001"] =
            [
                new("SERVICE-002", "VIN-SERVICE-001", "maintenance-record.pdf", "Maintenance Record", new DateTime(2025, 4, 12), "https://service.example/documents/SERVICE-002", "Service"),
                new("SERVICE-003", "VIN-SERVICE-001", "inspection-report.pdf", "Inspection Report", new DateTime(2025, 4, 12), "https://service.example/documents/SERVICE-003", "Service")
            ],
            ["VIN-FAIL-SALES"] =
            [
                new("SERVICE-004", "VIN-FAIL-SALES", "maintenance-record.pdf", "Maintenance Record", new DateTime(2025, 5, 18), "https://service.example/documents/SERVICE-004", "Service")
            ]
        };

    public static IResult GetResponse(string sourceSystem, string vin, bool fail)
    {
        var shouldFail = fail ||
            (sourceSystem == "Sales" && vin.Equals("VIN-FAIL-SALES", StringComparison.OrdinalIgnoreCase)) ||
            (sourceSystem == "Service" && vin.Equals("VIN-FAIL-SERVICE", StringComparison.OrdinalIgnoreCase));

        if (shouldFail)
        {
            return Results.Problem(
                title: $"{sourceSystem} System unavailable",
                detail: "Failure simulation was requested for this mock system.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var documents = sourceSystem == "Sales"
            ? SalesDocuments
            : ServiceDocuments;

        return documents.TryGetValue(vin, out var matchingDocuments)
            ? Results.Ok(matchingDocuments)
            : Results.NotFound(new { message = $"No {sourceSystem.ToLowerInvariant()} documents found for VIN '{vin}'." });
    }

    private sealed record MockDocument(
        string Id,
        string Vin,
        string FileName,
        string DocumentType,
        DateTime DocumentDate,
        string DownloadUrl,
        string SourceSystem);
}
