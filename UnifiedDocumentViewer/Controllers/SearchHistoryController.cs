using Microsoft.AspNetCore.Mvc;
using UnifiedDocumentViewer.Data;

namespace UnifiedDocumentViewer.Controllers;

[ApiController]
[Route("api/search-history")]
public sealed class SearchHistoryController(ISearchHistoryRepository searchHistoryRepository) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<SearchHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string? vin,
        CancellationToken cancellationToken)
    {
        var attempts = await searchHistoryRepository.GetAsync(
            string.IsNullOrWhiteSpace(vin) ? null : vin.Trim().ToUpperInvariant(),
            cancellationToken);

        var response = attempts
            .Select(attempt => new SearchHistoryResponse(
                attempt.Id,
                attempt.Vin,
                attempt.SearchedAtUtc,
                attempt.Status,
                attempt.DocumentsReturned,
                attempt.ErrorMessage))
            .ToArray();

        return Ok(response);
    }
}
