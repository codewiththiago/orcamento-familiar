using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Infrastructure.Parsers;

public class ImportParserFactory : IImportParserFactory
{
    private readonly IEnumerable<ITransactionImportParser> _parsers;

    public ImportParserFactory(IEnumerable<ITransactionImportParser> parsers) => _parsers = parsers;

    public ITransactionImportParser? GetParser(ImportFormat format, string? institution)
    {
        var candidates = _parsers.Where(p => p.Format == format).ToList();
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        if (!string.IsNullOrWhiteSpace(institution))
        {
            var institutionNameMatch = candidates.FirstOrDefault(c =>
                c.GetType().Name.Contains(institution, StringComparison.OrdinalIgnoreCase));
            if (institutionNameMatch != null) return institutionNameMatch;
        }

        return candidates[0];
    }
}