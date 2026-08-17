using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OrcamentoFamiliar.Application.DTOs.Imports;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Infrastructure.Parsers;

public partial class OfxTransactionParser : ITransactionImportParser
{
    public ImportFormat Format => ImportFormat.Ofx;

    private static readonly Regex TransactionBlockRegex =
        new(@"<STMTTRN>\s*(.*?)\s*</STMTTRN>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<List<ParsedTransaction>> ParseAsync(Stream file, string? institution, CancellationToken ct = default)
    {
        using var reader = new StreamReader(file, Encoding.UTF8, true);
        var content = reader.ReadToEnd();
        if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);

        content = Regex.Replace(content, @"<\?xml[^>]*\?>", string.Empty, RegexOptions.IgnoreCase);

        var results = new List<ParsedTransaction>();

        foreach (Match match in TransactionBlockRegex.Matches(content))
        {
            if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);

            var block = match.Groups[1].Value;
            var type = GetTag(block, "TRNTYPE");
            var dtPosted = GetTag(block, "DTPOSTED");
            var amount = GetTag(block, "TRNAMT");
            var fitId = GetTag(block, "FITID");
            var name = GetTag(block, "NAME");
            var memo = GetTag(block, "MEMO");

            var description = string.IsNullOrWhiteSpace(name) ? memo : name;
            if (string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(amount))
                continue;

            if (!TryParseAmount(amount, out var value))
                continue;

            var date = ParseOfxDate(dtPosted);
            if (date == null)
                continue;

            var typeEnum = ResolveType(type, value);

            results.Add(new ParsedTransaction
            {
                Description = Clean(description),
                Memo = string.IsNullOrWhiteSpace(memo) ? null : Clean(memo),
                Amount = Math.Abs(value),
                TransactionDate = date.Value,
                Type = typeEnum,
                ExternalId = string.IsNullOrWhiteSpace(fitId) ? null : fitId.Trim()
            });
        }

        return Task.FromResult(results);
    }

    private static string GetTag(string block, string tag)
    {
        var match = Regex.Match(block, $"<{tag}>(.*?)</{tag}>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static DateTime? ParseOfxDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var value = raw.Trim();
        var bracketIndex = value.IndexOf('[');
        if (bracketIndex > 0)
            value = value[..bracketIndex].Trim();

        string[] formats = ["yyyyMMddHHmmss", "yyyyMMddHHmmss.FFF", "yyyyMMdd", "yyyy-MM-dd"];

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);

        return null;
    }

    private static bool TryParseAmount(string raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var s = raw.Trim().Replace("R$", "").Replace(" ", "").Replace("+", "");
        if (s.Contains(','))
        {
            var normalized = s.Replace(".", "").Replace(",", ".");
            return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static TransactionType ResolveType(string type, decimal value)
    {
        var normalized = type.Trim().ToUpperInvariant();
        if (normalized.Contains("CREDIT"))
            return TransactionType.Income;
        if (normalized.Contains("DEBIT"))
            return TransactionType.Expense;

        return value < 0 ? TransactionType.Expense : TransactionType.Income;
    }

    private static string Clean(string s) =>
        Regex.Replace(s.Trim(), @"\s+", " ");
}