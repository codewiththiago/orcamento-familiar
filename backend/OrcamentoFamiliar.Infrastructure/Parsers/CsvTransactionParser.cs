using System.Globalization;
using System.Text;
using OrcamentoFamiliar.Application.DTOs.Imports;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Infrastructure.Parsers;

public class CsvTransactionParser : ITransactionImportParser
{
    public ImportFormat Format => ImportFormat.Csv;

    public Task<List<ParsedTransaction>> ParseAsync(Stream file, string? institution, CancellationToken ct = default)
    {
        var lines = ReadLines(file, ct);
        if (lines.Count == 0)
            return Task.FromResult(new List<ParsedTransaction>());

        var delimiter = DetectDelimiter(lines);
        var rows = lines.Select(l => SplitLine(l, delimiter)).ToList();

        var headerIndex = FindHeaderIndex(rows);
        var columnMap = headerIndex >= 0 ? MapColumns(rows[headerIndex]) : new ColumnMap();

        var results = new List<ParsedTransaction>();
        var referenceYear = DateTime.UtcNow.Year;

        for (var i = headerIndex + 1; i < rows.Count; i++)
        {
            if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);

            var cells = rows[i];
            if (cells.Count < 2)
                continue;

            var firstCell = cells[0].Trim().ToLowerInvariant();
            if (firstCell is "data" or "date" or "data de lanzamento")
                continue;

            var dateText = columnMap.Date >= 0 ? Get(cells, columnMap.Date) : null;
            var description = columnMap.Description >= 0 ? Get(cells, columnMap.Description) : null;
            if (string.IsNullOrWhiteSpace(description))
                description = columnMap.Type >= 0 ? null : JoinCells(cells);

            string? creditText = columnMap.Credit >= 0 ? Get(cells, columnMap.Credit) : null;
            string? debitText = columnMap.Debit >= 0 ? Get(cells, columnMap.Debit) : null;
            string? valueText = columnMap.Value >= 0 ? Get(cells, columnMap.Value) : null;
            var typeText = columnMap.Type >= 0 ? Get(cells, columnMap.Type) : null;
            var externalId = columnMap.ExternalId >= 0 ? Get(cells, columnMap.ExternalId) : null;

            if (string.IsNullOrWhiteSpace(description))
                continue;

            var date = TryParseDate(dateText, referenceYear);
            if (date == null)
                continue;

            var credit = ParseAmount(creditText);
            var debit = ParseAmount(debitText);
            var value = (columnMap.Credit >= 0 || columnMap.Debit >= 0) ? credit - debit : ParseAmount(valueText);
            if (value == 0m)
                continue;

            var type = ResolveType(value, typeText);
            var memo = description;

            results.Add(new ParsedTransaction
            {
                Description = description.Trim(),
                Memo = memo.Trim(),
                Amount = Math.Abs(value),
                TransactionDate = date.Value,
                Type = type,
                ExternalId = string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim()
            });
        }

        return Task.FromResult(results);
    }

    private static List<string> ReadLines(Stream file, CancellationToken ct)
    {
        var lines = new List<string>();
        using var reader = new StreamReader(file, Encoding.UTF8, true);
        while (reader.ReadLine() is { } line)
        {
            if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            lines.Add(line.Trim());
        }
        return lines;
    }

    private static char DetectDelimiter(List<string> lines)
    {
        var sample = string.Join('\n', lines.Take(20));
        var semicolon = sample.Count(c => c == ';');
        var comma = sample.Count(c => c == ',');
        var tab = sample.Count(c => c == '\t');
        if (semicolon > comma && semicolon > tab) return ';';
        if (tab > comma && tab > semicolon) return '\t';
        return ',';
    }

    private static List<string> SplitLine(string line, char delimiter)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == delimiter && !inQuotes)
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        cells.Add(current.ToString());
        return cells;
    }

    private static readonly string[] DateTokens = ["data", "date", "lançamento", "lancamento", "movimento", "dt", "fecha"];
    private static readonly string[] DescriptionTokens = ["descricao", "descrição", "desc", "estabelecimento", "historico", "histórico", "nome", "name", "title", "pagador", "beneficiario"];
    private static readonly string[] ValueTokens = ["valor", "value", "amount", "montante"];
    private static readonly string[] CreditTokens = ["credito", "crédito", "credit"];
    private static readonly string[] DebitTokens = ["debito", "débito", "debit", "saidas", "saídas"];
    private static readonly string[] TypeTokens = ["tipo", "type", "sinal", "dc", "credito/debito", "tipo de movimentacao"];
    private static readonly string[] ExternalIdTokens = ["identificador", "id", "codigo", "código", "code", "fitid", "nro", "numero"];

    private static int FindHeaderIndex(List<List<string>> rows)
    {
        var maxSearch = Math.Min(rows.Count, 15);
        for (var i = 0; i < maxSearch; i++)
        {
            var cells = rows[i];
            var normalized = cells.Select(NormalizeToken).ToList();
            var recognized = normalized.Count(t => IsKnownColumn(t));
            if (recognized >= 2)
                return i;
        }
        return -1;
    }

    private static ColumnMap MapColumns(List<string> header)
    {
        var map = new ColumnMap();
        var normalized = header.Select(NormalizeToken).ToList();

        for (var i = 0; i < normalized.Count; i++)
        {
            var token = normalized[i];
            if (token.Length == 0) continue;

            if (ContainsAny(token, DateTokens)) map.Date = i;
            else if (ContainsAny(token, DescriptionTokens)) map.Description = i;
            else if (ContainsAny(token, ValueTokens)) map.Value = i;
            else if (ContainsAny(token, CreditTokens)) map.Credit = i;
            else if (ContainsAny(token, DebitTokens)) map.Debit = i;
            else if (ContainsAny(token, TypeTokens)) map.Type = i;
            else if (ContainsAny(token, ExternalIdTokens)) map.ExternalId = i;
        }

        return map;
    }

    private static bool IsKnownColumn(string token)
    {
        if (token.Length == 0) return false;
        return ContainsAny(token, DateTokens)
            || ContainsAny(token, DescriptionTokens)
            || ContainsAny(token, ValueTokens)
            || ContainsAny(token, CreditTokens)
            || ContainsAny(token, DebitTokens)
            || ContainsAny(token, TypeTokens)
            || ContainsAny(token, ExternalIdTokens);
    }

    private static bool ContainsAny(string token, string[] tokens) =>
        tokens.Any(t => token.Contains(t));

    private static string NormalizeToken(string raw)
    {
        var s = raw.Trim().ToLowerInvariant();
        s = TransactionNormalizer.RemoveDiacritics(s);
        s = new string(s.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
        return string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? Get(List<string> cells, int index) =>
        index >= 0 && index < cells.Count ? cells[index].Trim() : null;

    private static string JoinCells(List<string> cells) =>
        string.Join(' ', cells.Where(c => !string.IsNullOrWhiteSpace(c))).Trim();

    private static DateTime? TryParseDate(string? raw, int referenceYear)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var value = raw.Trim();

        string[] formats =
        [
            "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy",
            "dd/MM/yy", "d/MM/yy", "dd/M/yy", "d/M/yy",
            "MM/dd/yyyy", "M/dd/yyyy", "MM/d/yyyy", "M/d/yyyy",
            "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd",
            "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy",
            "dd/MM", "d/MM", "dd/M"
        ];

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            if (value.Length <= 5)
                parsed = new DateTime(referenceYear, parsed.Month, parsed.Day);
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedGeneral))
            return DateTime.SpecifyKind(parsedGeneral.Date, DateTimeKind.Utc);

        return null;
    }

    private static decimal ParseAmount(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0m;

        var value = raw.Trim();
        if (value.StartsWith('(') && value.EndsWith(')'))
            value = "-" + value[1..^1];

        value = value.Replace("R$", "").Replace("r$", "").Replace(" ", "").Trim();

        if (value.Contains(','))
        {
            var parts = value.Split(',');
            var decimalPart = parts[^1];
            var intPart = string.Join("", parts.Take(parts.Length - 1)).Replace(".", "");
            value = intPart + "." + decimalPart;
        }
        else if (value.Count(c => c == '.') > 1)
        {
            value = value.Replace(".", "");
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return 0m;
    }

    private static TransactionType ResolveType(decimal signedValue, string? typeText)
    {
        if (!string.IsNullOrWhiteSpace(typeText))
        {
            var t = TransactionNormalizer.RemoveDiacritics(typeText.ToLowerInvariant());
            if (t.Contains("credito") || t == "c" || (t.Length == 1 && t[0] == 'c') || t.Contains("entrada"))
                return TransactionType.Income;
            if (t.Contains("debito") || t == "d" || (t.Length == 1 && t[0] == 'd') || t.Contains("saida"))
                return TransactionType.Expense;
        }

        return signedValue < 0 ? TransactionType.Expense : TransactionType.Income;
    }

    private class ColumnMap
    {
        public int Date { get; set; } = -1;
        public int Description { get; set; } = -1;
        public int Value { get; set; } = -1;
        public int Credit { get; set; } = -1;
        public int Debit { get; set; } = -1;
        public int Type { get; set; } = -1;
        public int ExternalId { get; set; } = -1;
    }
}