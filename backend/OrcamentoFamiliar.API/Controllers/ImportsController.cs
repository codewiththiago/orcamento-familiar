using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcamentoFamiliar.Application.DTOs.Imports;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.API.Controllers;

[ApiController]
[Route("api/imports")]
[Authorize]
public class ImportsController : ControllerBase
{
    private readonly IImportService _importService;
    public ImportsController(IImportService importService) => _importService = importService;

    [HttpPost("preview")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Preview(
        [FromForm] IFormFile file,
        [FromForm] int financialAccountId,
        [FromForm] int format = 0,
        [FromForm] string? institution = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Envie um arquivo" });

        if (!Enum.IsDefined(typeof(ImportFormat), format))
            return BadRequest(new { message = "Formato de importação inválido" });

        using var stream = file.OpenReadStream();
        var result = await _importService.PreviewAsync(stream, file.FileName, (ImportFormat)format, institution, financialAccountId);
        return Ok(result);
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmImportRequestDto dto)
    {
        var result = await _importService.ConfirmAsync(dto);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory()
    {
        var result = await _importService.GetHistoryAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _importService.GetByIdAsync(id);
        return Ok(result);
    }
}