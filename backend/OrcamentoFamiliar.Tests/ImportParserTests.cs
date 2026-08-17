using System.Text;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Parsers;

namespace OrcamentoFamiliar.Tests;

public class CsvTransactionParserTests
{
    private static Stream Stream(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task NubankStyleCsv_ParsesExpensesAndIncomes()
    {
        var csv = """
            data,valor,identificador,descricao
            2026-08-01,"-150,90",br-xyz-1,Supermercado Extra
            2026-08-02,"2500,00",br-abc-2,Depósito recebido
            2026-08-03,"-25,00",br-zzz-3,OpenRouter
            """;

        var parser = new CsvTransactionParser();
        var txns = await parser.ParseAsync(Stream(csv), "nubank");

        Assert.Equal(3, txns.Count);

        Assert.Equal(TransactionType.Expense, txns[0].Type);
        Assert.Equal(150.90m, txns[0].Amount);
        Assert.Equal("Supermercado Extra", txns[0].Description);
        Assert.Equal("br-xyz-1", txns[0].ExternalId);
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), txns[0].TransactionDate);

        Assert.Equal(TransactionType.Income, txns[1].Type);
        Assert.Equal(2500m, txns[1].Amount);
        Assert.Equal(new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc), txns[1].TransactionDate);

        Assert.Equal(TransactionType.Expense, txns[2].Type);
        Assert.Equal(25m, txns[2].Amount);
    }

    [Fact]
    public async Task SemicolonDelimitedC6Style_ParsesCreditDebitColumns()
    {
        var csv = """
            Data;Histórico;Crédito;Débito
            10/08/2026;PIX RECEBIDO;500,00;;
            11/08/2026;POSTO IPIRANGA;;120,50;
            """;

        var parser = new CsvTransactionParser();
        var txns = await parser.ParseAsync(Stream(csv), "c6");

        Assert.Equal(2, txns.Count);
        Assert.Equal(TransactionType.Income, txns[0].Type);
        Assert.Equal(500m, txns[0].Amount);
        Assert.Equal(TransactionType.Expense, txns[1].Type);
        Assert.Equal(120.50m, txns[1].Amount);
    }

    [Fact]
    public async Task BillingStatementsSkipped()
    {
        var csv = """
            Saldo inicial: 1000,00
            Data;Descrição;Valor
            01/08/2026;MERCADO;-50,00
            """;

        var parser = new CsvTransactionParser();
        var txns = await parser.ParseAsync(Stream(csv), null);

        Assert.Single(txns);
        Assert.Equal(50m, txns[0].Amount);
        Assert.Equal(TransactionType.Expense, txns[0].Type);
    }
}

public class OfxTransactionParserTests
{
    private static Stream Stream(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task ParsesStandardOfxStatement()
    {
        var ofx = """
            OFXHEADER:100
            DATA:OFXSGML
            <OFX>
            <BANKMSGSRSV1>
            <STMTTRNRS><STMTRS>
            <BANKTRANLIST>
            <STMTTRN>
              <TRNTYPE>DEBIT</TRNTYPE>
              <DTPOSTED>20260810010000[-3:BRT]</DTPOSTED>
              <TRNAMT>-89,90</TRNAMT>
              <FITID>2026081000001</FITID>
              <NAME>MERCADO EXTRA</NAME>
              <MEMO>COMPRA ONLINE</MEMO>
            </STMTTRN>
            <STMTTRN>
              <TRNTYPE>CREDIT</TRNTYPE>
              <DTPOSTED>20260815120000[-3:BRT]</DTPOSTED>
              <TRNAMT>1000,00</TRNAMT>
              <FITID>2026081500002</FITID>
              <NAME>PIX RECEBIDO</NAME>
            </STMTTRN>
            </BANKTRANLIST>
            </STMTRS></STMTTRNRS>
            </BANKMSGSRSV1>
            </OFX>
            """;

        var parser = new OfxTransactionParser();
        var txns = await parser.ParseAsync(Stream(ofx), null);

        Assert.Equal(2, txns.Count);

        Assert.Equal(TransactionType.Expense, txns[0].Type);
        Assert.Equal(89.90m, txns[0].Amount);
        Assert.Equal("MERCADO EXTRA", txns[0].Description);
        Assert.Equal("2026081000001", txns[0].ExternalId);
        Assert.Equal(new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc), txns[0].TransactionDate);

        Assert.Equal(TransactionType.Income, txns[1].Type);
        Assert.Equal(1000m, txns[1].Amount);
        Assert.Equal(new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), txns[1].TransactionDate);
    }

    [Fact]
    public async Task IgnoresMalformedTransactions()
    {
        var ofx = """
            <OFX>
            <STMTTRN>
              <TRNTYPE>DEBIT</TRNTYPE>
              <DTPOSTED>20260810</DTPOSTED>
              <TRNAMT>NAO-EH-VALOR</TRNAMT>
              <NAME>DESCRICAO SEM VALOR</NAME>
            </STMTTRN>
            <STMTTRN>
              <TRNTYPE>DEBIT</TRNTYPE>
              <DTPOSTED>20260810</DTPOSTED>
              <TRNAMT>-10,00</TRNAMT>
              <NAME>VALIDA</NAME>
            </STMTTRN>
            </OFX>
            """;

        var parser = new OfxTransactionParser();
        var txns = await parser.ParseAsync(Stream(ofx), null);

        Assert.Single(txns);
        Assert.Equal("VALIDA", txns[0].Description);
    }
}

public class TransactionNormalizerTests
{
    [Theory]
    [InlineData("mercadinho são joão", "MERCADINHO SAO JOAO")]
    [InlineData("  ifood *entrega ", "IFOOD ENTREGA")]
    [InlineData("débito em conta", "DEBITO EM CONTA")]
    public void Normalize_ClearsAccentsCaseAndWhitespace(string input, string expected)
    {
        Assert.Equal(expected, TransactionNormalizer.Normalize(input));
    }

    [Fact]
    public void BuildHash_IsDeterministicForSameInput()
    {
        var date = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var h1 = TransactionNormalizer.BuildHash(1, null, date, 150.90m, "MERCADO X");
        var h2 = TransactionNormalizer.BuildHash(1, null, date, 150.90m, "MERCADO X");
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void BuildHash_ExternalIdChangesHash()
    {
        var date = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var h1 = TransactionNormalizer.BuildHash(1, null, date, 150.90m, "MERCADO X");
        var h2 = TransactionNormalizer.BuildHash(1, "fit-123", date, 150.90m, "MERCADO X");
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void BuildHash_ExternalIdIsPreferredOverContent()
    {
        // same external id but different content => same hash (external id is authoritative)
        var date = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var h1 = TransactionNormalizer.BuildHash(1, "fit-123", date, 10m, "A");
        var h2 = TransactionNormalizer.BuildHash(1, "fit-123", date, 999m, "B");
        Assert.Equal(h1, h2);
    }
}