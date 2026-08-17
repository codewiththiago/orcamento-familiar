using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace OrcamentoFamiliar.Infrastructure.Parsers;

public static class TransactionNormalizer
{
    public static string Normalize(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return string.Empty;

        var s = description.ToUpperInvariant();
        s = RemoveDiacritics(s);
        s = new string(s.Select(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) ? c : ' ').ToArray());
        s = string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return s.Trim();
    }

    public static string BuildHash(int financialAccountId, string? externalId, DateTime transactionDate, decimal amount, string normalizedDescription)
    {
        string raw;
        if (!string.IsNullOrWhiteSpace(externalId))
        {
            raw = $"{financialAccountId}|{externalId.Trim()}";
        }
        else
        {
            var date = transactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var value = amount.ToString("0.00", CultureInfo.InvariantCulture);
            raw = $"{financialAccountId}|{date}|{value}|{normalizedDescription}";
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    public static string BuildFileHash(IEnumerable<string> lines)
    {
        var content = string.Join('\n', lines);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }

    public static string RemoveDiacritics(string s)
    {
        var normalized = s.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}