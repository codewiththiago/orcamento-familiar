using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcamentoFamiliar.Application.DTOs.Transactions;
using OrcamentoFamiliar.Application.Interfaces;

namespace OrcamentoFamiliar.API.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    public TransactionsController(ITransactionService transactionService) => _transactionService = transactionService;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? accountId,
        [FromQuery] int? categoryId,
        [FromQuery] int? type,
        [FromQuery] int? limit)
    {
        var result = await _transactionService.QueryAsync(new TransactionQueryDto
        {
            From = from,
            To = to,
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type.HasValue ? (Domain.Enums.TransactionType)type.Value : null,
            Limit = limit ?? 500
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _transactionService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        var result = await _transactionService.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionDto dto)
    {
        var result = await _transactionService.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool deleteFuture = false)
    {
        await _transactionService.DeleteAsync(id, deleteFuture);
        return NoContent();
    }
}