using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcamentoFamiliar.Application.Interfaces;

namespace OrcamentoFamiliar.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    public DashboardController(IBudgetService budgetService) => _budgetService = budgetService;

    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int? year)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var result = await _budgetService.GetDashboardAsync(targetYear);
        return Ok(result);
    }
}
