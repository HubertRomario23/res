using Microsoft.AspNetCore.Mvc;
using ResultViewer.Application.DTOs;
using ResultViewer.Application.Interfaces;

namespace ResultViewer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RunsController : ControllerBase
{
    private readonly IRunQueryService _queryService;
    private readonly ILogger<RunsController> _logger;

    public RunsController(IRunQueryService queryService, ILogger<RunsController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/runs?host=xxx&amp;pdc=yyy&amp;runId=zzz
    /// Retrieves a single test run by composite key (DB-first, then NightBatch fallback).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TestRunDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(
        [FromQuery] string host,
        [FromQuery] string pdc,
        [FromQuery] string runId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(pdc) || string.IsNullOrWhiteSpace(runId))
            return BadRequest(new ErrorResponseDto("Missing required parameters", "host, pdc, and runId are required.", HttpContext.TraceIdentifier));

        _logger.LogInformation("GET /api/runs - Host={Host}, Pdc={Pdc}, RunId={RunId}", host, pdc, runId);

        var result = await _queryService.GetTestRunAsync(host, pdc, runId, ct);

        if (result is null)
            return NotFound(new ErrorResponseDto("run not found",
                $"No run found for Host={host}, Pdc={pdc}, RunId={runId}",
                HttpContext.TraceIdentifier));

        return Ok(result);
    }

    /// <summary>
    /// GET /api/runs/list?page=1&amp;pageSize=20&amp;host=xxx&amp;result=Passed
    /// Returns paginated list of test runs with optional filters.
    /// </summary>
    [HttpGet("list")]
    [ProducesResponseType(typeof(PagedResultDto<TestRunDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRuns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? host = null,
        [FromQuery] string? pdc = null,
        [FromQuery] string? result = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        _logger.LogInformation("GET /api/runs/list - Page={Page}, PageSize={PageSize}, Host={Host}, Pdc={Pdc}, Result={Result}",
            page, pageSize, host, pdc, result);

        var pagedResult = await _queryService.GetAllRunsAsync(page, pageSize, host, pdc, result, fromDate, toDate, ct);

        return Ok(pagedResult);
    }
}
