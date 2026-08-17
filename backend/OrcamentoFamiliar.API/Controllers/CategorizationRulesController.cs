using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcamentoFamiliar.Application.DTOs.CategorizationRules;
using OrcamentoFamiliar.Application.Interfaces;

namespace OrcamentoFamiliar.API.Controllers;

[ApiController]
[Route("api/categorization-rules")]
[Authorize]
public class CategorizationRulesController : ControllerBase
{
    private readonly ICategorizationService _categorizationService;
    public CategorizationRulesController(ICategorizationService categorizationService) => _categorizationService = categorizationService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categorizationService.GetRulesAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategorizationRuleDto dto)
    {
        var result = await _categorizationService.CreateRuleAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategorizationRuleDto dto)
    {
        var result = await _categorizationService.UpdateRuleAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _categorizationService.DeleteRuleAsync(id);
        return NoContent();
    }
}