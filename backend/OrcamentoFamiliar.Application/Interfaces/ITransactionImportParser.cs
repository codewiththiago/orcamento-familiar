using OrcamentoFamiliar.Application.DTOs.Imports;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Application.Interfaces;

public interface ITransactionImportParser
{
    ImportFormat Format { get; }
    Task<List<ParsedTransaction>> ParseAsync(Stream file, string? institution, CancellationToken ct = default);
}

public interface IImportParserFactory
{
    ITransactionImportParser? GetParser(ImportFormat format, string? institution);
}