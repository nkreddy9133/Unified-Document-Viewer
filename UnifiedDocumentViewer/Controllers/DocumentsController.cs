using Microsoft.AspNetCore.Mvc;
using UnifiedDocumentViewer.Models;
using UnifiedDocumentViewer.Services;

namespace UnifiedDocumentViewer.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(IDocumentAggregationService aggregationService) : ControllerBase
{
    [HttpGet("{vin}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(DocumentSearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(DocumentSearchResult), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDocuments(
        string vin,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await aggregationService.SearchAsync(vin, cancellationToken);

            return result.HasSuccessfulSource
                ? Ok(result)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
