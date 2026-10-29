using Microsoft.AspNetCore.Mvc;
using UnifiedDocumentViewer.Models;

namespace UnifiedDocumentViewer.Controllers;

[ApiController]
[Route("mock")]
public sealed class MockSystemsController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, Document[]> SalesDocuments =
        new Dictionary<string, Document[]>(StringComparer.OrdinalIgnoreCase)
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

    private static readonly IReadOnlyDictionary<string, Document[]> ServiceDocuments =
        new Dictionary<string, Document[]>(StringComparer.OrdinalIgnoreCase)
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

    [HttpGet("sales/documents/{vin}")]
    [ProducesResponseType(typeof(IReadOnlyList<Document>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetSalesDocuments(string vin, [FromQuery] bool fail = false)
    {
        return GetDocuments("Sales", vin, fail, SalesDocuments);
    }

    [HttpGet("service/documents/{vin}")]
    [ProducesResponseType(typeof(IReadOnlyList<Document>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetServiceDocuments(string vin, [FromQuery] bool fail = false)
    {
        return GetDocuments("Service", vin, fail, ServiceDocuments);
    }

    [HttpGet("health")]
    public IActionResult GetHealth() => Ok(new { status = "healthy" });

    private IActionResult GetDocuments(
        string sourceSystem,
        string vin,
        bool fail,
        IReadOnlyDictionary<string, Document[]> documentsByVin)
    {
        var shouldFail = fail ||
            (sourceSystem == "Sales" && vin.Equals("VIN-FAIL-SALES", StringComparison.OrdinalIgnoreCase)) ||
            (sourceSystem == "Service" && vin.Equals("VIN-FAIL-SERVICE", StringComparison.OrdinalIgnoreCase));

        if (shouldFail)
        {
            return Problem(
                title: $"{sourceSystem} System unavailable",
                detail: "Failure simulation was requested for this mock system.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return documentsByVin.TryGetValue(vin, out var documents)
            ? Ok(documents)
            : NotFound(new { message = $"No {sourceSystem.ToLowerInvariant()} documents found for VIN '{vin}'." });
    }
}
