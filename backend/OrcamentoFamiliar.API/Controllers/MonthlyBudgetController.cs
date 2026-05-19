using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcamentoFamiliar.Application.DTOs.Budget;
using OrcamentoFamiliar.Application.Interfaces;

namespace OrcamentoFamiliar.API.Controllers;

[ApiController]
[Route("api/budget")]
[Authorize]
public class MonthlyBudgetController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    public MonthlyBudgetController(IBudgetService budgetService) => _budgetService = budgetService;

    [HttpGet("{year}/{month}")]
    public async Task<IActionResult> GetOrCreate(int year, int month)
    {
        var result = await _budgetService.GetOrCreateMonthlyBudgetAsync(year, month);
        return Ok(result);
    }

    [HttpPut("{year}/{month}/salary")]
    public async Task<IActionResult> UpdateSalary(int year, int month, [FromBody] UpdateSalaryDto dto)
    {
        var result = await _budgetService.UpdateSalaryAsync(year, month, dto);
        return Ok(result);
    }

    // Extra Incomes
    [HttpPost("extra-income")]
    public async Task<IActionResult> AddExtraIncome([FromBody] CreateExtraIncomeDto dto)
    {
        var result = await _budgetService.AddExtraIncomeAsync(dto);
        return Ok(result);
    }

    [HttpPut("extra-income/{id}")]
    public async Task<IActionResult> UpdateExtraIncome(int id, [FromBody] UpdateExtraIncomeDto dto)
    {
        var result = await _budgetService.UpdateExtraIncomeAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("extra-income/{id}")]
    public async Task<IActionResult> DeleteExtraIncome(int id)
    {
        await _budgetService.DeleteExtraIncomeAsync(id);
        return NoContent();
    }

    // Fixed Expenses
    [HttpPost("fixed-expense")]
    public async Task<IActionResult> AddFixedExpense([FromBody] CreateFixedExpenseDto dto)
    {
        var result = await _budgetService.AddFixedExpenseAsync(dto);
        return Ok(result);
    }

    [HttpPut("fixed-expense/{id}")]
    public async Task<IActionResult> UpdateFixedExpense(int id, [FromBody] UpdateFixedExpenseDto dto)
    {
        var result = await _budgetService.UpdateFixedExpenseAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("fixed-expense/{id}")]
    public async Task<IActionResult> DeleteFixedExpense(int id)
    {
        await _budgetService.DeleteFixedExpenseAsync(id);
        return NoContent();
    }

    // Credit Card Launches
    [HttpPost("launch")]
    public async Task<IActionResult> AddLaunch([FromBody] CreateCreditCardLaunchDto dto)
    {
        var result = await _budgetService.AddCreditCardLaunchAsync(dto);
        return Ok(result);
    }

    [HttpPut("launch/{id}")]
    public async Task<IActionResult> UpdateLaunch(int id, [FromBody] UpdateCreditCardLaunchDto dto)
    {
        var result = await _budgetService.UpdateCreditCardLaunchAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("launch/{id}")]
    public async Task<IActionResult> DeleteLaunch(int id, [FromQuery] bool deleteAll = false)
    {
        await _budgetService.DeleteCreditCardLaunchAsync(id, deleteAll);
        return NoContent();
    }

    // Category Summary
    [HttpGet("{budgetId}/category-summary")]
    public async Task<IActionResult> GetCategorySummary(int budgetId)
    {
        var result = await _budgetService.GetCategorySummaryAsync(budgetId);
        return Ok(result);
    }

}
