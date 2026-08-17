using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcamentoFamiliar.Application.Interfaces;

namespace OrcamentoFamiliar.API.Controllers;

[ApiController]
[Route("api/insights")]
[Authorize]
public class InsightsController : ControllerBase
{
    private readonly IInsightsService _insightsService;
    public InsightsController(IInsightsService insightsService) => _insightsService = insightsService;

    [HttpGet("monthly/{year}/{month}")]
    public async Task<IActionResult> GetMonthly(int year, int month)
    {
        var result = await _insightsService.GetMonthlyAsync(year, month);
        return Ok(result);
    }

    [HttpGet("commitments")]
    public async Task<IActionResult> GetCommitments([FromQuery] int year, [FromQuery] int month, [FromQuery] int months = 6)
    {
        var result = await _insightsService.GetCommitmentsAsync(year, month, months);
        return Ok(result);
    }
}