using LocalFinanceManager.DTOs;
using LocalFinanceManager.Services.Import;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocalFinanceManager.Tests.Unit.Import;

[TestFixture]
public class CsvImportParserTests
{
    private Mock<ILogger<CsvImportParser>> _mockLogger = null!;
    private CsvImportParser _parser = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<CsvImportParser>>();
        _parser = new CsvImportParser(_mockLogger.Object);
    }

    #region Delimiter Detection Tests

    [Test]
    public async Task DetectColumnsAsync_CommaDelimited_ReturnsCorrectColumns()
    {
        // Arrange
        var csv = "Date,Amount,Description,Counterparty\n2026-01-01,100.50,Test,Party";

        // Act
        var columns = await _parser.DetectColumnsAsync(csv);

        // Assert
        Assert.That(columns, Has.Count.EqualTo(4));
        Assert.That(columns[0], Is.EqualTo("Date"));
        Assert.That(columns[1], Is.EqualTo("Amount"));
        Assert.That(columns[2], Is.EqualTo("Description"));
        Assert.That(columns[3], Is.EqualTo("Counterparty"));
    }

    [Test]
    public async Task DetectColumnsAsync_SemicolonDelimited_ReturnsCorrectColumns()
    {
        // Arrange
        var csv = "Datum;Bedrag (EUR);Naam / Omschrijving;Mededelingen\n20230215;50,00;Test;Details";

        // Act
        var columns = await _parser.DetectColumnsAsync(csv);

        // Assert
        Assert.That(columns, Has.Count.EqualTo(4));
        Assert.That(columns[0], Is.EqualTo("Datum"));
        Assert.That(columns[1], Is.EqualTo("Bedrag (EUR)"));
        Assert.That(columns[2], Is.EqualTo("Naam / Omschrijving"));
        Assert.That(columns[3], Is.EqualTo("Mededelingen"));
    }

    [Test]
    public async Task DetectColumnsAsync_TabDelimited_ReturnsCorrectColumns()
    {
        // Arrange
        var csv = "Date\tAmount\tDescription\n2026-01-01\t100.50\tTest";

        // Act
        var columns = await _parser.DetectColumnsAsync(csv);

        // Assert
        Assert.That(columns, Has.Count.EqualTo(3));
        Assert.That(columns[0], Is.EqualTo("Date"));
        Assert.That(columns[1], Is.EqualTo("Amount"));
        Assert.That(columns[2], Is.EqualTo("Description"));
    }

    #endregion

    #region Auto-Detection Tests

    [Test]
    public async Task AutoDetectMappingAsync_StandardFormat_DetectsCorrectly()
    {
        // Arrange
        var csv = "Date,Amount,Description,Counterparty,ExternalId\n" +
                  "2026-01-01,100.50,Salary,Employer,TXN001";

        // Act
        var mapping = await _parser.AutoDetectMappingAsync(csv);

        // Assert
        Assert.That(mapping.DateColumn, Is.EqualTo("Date"));
        Assert.That(mapping.AmountColumn, Is.EqualTo("Amount"));
        Assert.That(mapping.DescriptionColumn, Is.EqualTo("Description"));
        Assert.That(mapping.CounterpartyColumn, Is.EqualTo("Counterparty"));
        Assert.That(mapping.ExternalIdColumn, Is.EqualTo("ExternalId"));
    }

    [Test]
    public async Task AutoDetectMappingAsync_DutchFormat_DetectsCorrectly()
    {
        // Arrange
        var csv = "Datum;Bedrag (EUR);Naam / Omschrijving;Mededelingen;Referentie\n" +
                  "20230215;50,00;Test;Details;REF123";

        // Act
        var mapping = await _parser.AutoDetectMappingAsync(csv);

        // Assert
        Assert.That(mapping.DateColumn, Is.EqualTo("Datum"));
        Assert.That(mapping.AmountColumn, Is.EqualTo("Bedrag (EUR)"));
        // "Naam / Omschrijving" contains "omschrijving" so detected as description
        // This is the first column that matches, so it takes precedence
        Assert.That(mapping.DescriptionColumn, Is.EqualTo("Naam / Omschrijving"));
        // User can manually adjust mapping in UI if needed
        Assert.That(mapping.ExternalIdColumn, Is.EqualTo("Referentie"));
    }

    #endregion

    #region Parsing Tests - Basic

    [Test]
    public async Task ParseAsync_SimpleCommaCsv_ParsesCorrectly()
    {
        // Arrange
        var csv = "Date,Amount,Description\n" +
                  "2026-01-01,100.50,Salary\n" +
                  "2026-01-02,-25.00,Groceries";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(2));

        Assert.That(transactions[0].Date, Is.EqualTo(new DateTime(2026, 1, 1)));
        Assert.That(transactions[0].Amount, Is.EqualTo(100.50m));
        Assert.That(transactions[0].Description, Is.EqualTo("Salary"));

        Assert.That(transactions[1].Date, Is.EqualTo(new DateTime(2026, 1, 2)));
        Assert.That(transactions[1].Amount, Is.EqualTo(-25.00m));
        Assert.That(transactions[1].Description, Is.EqualTo("Groceries"));
    }

    [Test]
    public async Task ParseAsync_WithQuotes_ParsesCorrectly()
    {
        // Arrange
        var csv = "\"Date\",\"Amount\",\"Description\"\n" +
                  "\"2026-01-01\",\"100.50\",\"Salary Payment\"\n" +
                  "\"2026-01-02\",\"-25.00\",\"Grocery Store\"";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(2));
        Assert.That(transactions[0].Description, Is.EqualTo("Salary Payment"));
        Assert.That(transactions[1].Description, Is.EqualTo("Grocery Store"));
    }

    [Test]
    public async Task ParseAsync_EmptyFile_ReturnsEmptyList()
    {
        // Arrange
        var csv = "";
        var mapping = new ColumnMappingDto();

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Is.Empty);
    }

    #endregion

    #region Parsing Tests - ING Format with Af Bij

    [Test]
    public async Task ParseAsync_INGFormatWithAfBij_ParsesCorrectly()
    {
        // Arrange
        var csv = "\"Datum\";\"Naam / Omschrijving\";\"Rekening\";\"Tegenrekening\";\"Code\";\"Af Bij\";\"Bedrag (EUR)\";\"Mutatiesoort\";\"Mededelingen\"\n" +
                  "\"20230215\";\"Hr G J Gielen\";\"NL43INGB0682659347\";\"NL79INGB0660730103\";\"GT\";\"Af\";\"50,00\";\"Online bankieren\";\"Test payment\"\n" +
                  "\"20230216\";\"Salary Corp\";\"NL43INGB0682659347\";\"NL12BANK0123456789\";\"GT\";\"Bij\";\"2500,00\";\"Overboeking\";\"Monthly salary\"";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Datum",
            AmountColumn = "Bedrag (EUR)",
            DescriptionColumn = "Mededelingen",
            CounterpartyColumn = "Naam / Omschrijving"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(2));

        // First transaction: "Af" (debit) should be negative
        Assert.That(transactions[0].Date, Is.EqualTo(new DateTime(2023, 2, 15)));
        Assert.That(transactions[0].Amount, Is.EqualTo(-50.00m));
        Assert.That(transactions[0].Description, Is.EqualTo("Test payment"));
        Assert.That(transactions[0].Counterparty, Is.EqualTo("Hr G J Gielen"));

        // Second transaction: "Bij" (credit) should be positive
        Assert.That(transactions[1].Date, Is.EqualTo(new DateTime(2023, 2, 16)));
        Assert.That(transactions[1].Amount, Is.EqualTo(2500.00m));
        Assert.That(transactions[1].Description, Is.EqualTo("Monthly salary"));
        Assert.That(transactions[1].Counterparty, Is.EqualTo("Salary Corp"));
    }

    [Test]
    public async Task ParseAsync_AfBijWithNegativeAmount_HandlesCorrectly()
    {
        // Arrange - edge case where amount already has sign
        var csv = "Date;Af Bij;Amount;Description\n" +
                  "2026-01-01;Af;-50.00;Test\n" +
                  "2026-01-02;Bij;100.00;Test2";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(2));
        Assert.That(transactions[0].Amount, Is.EqualTo(-50.00m)); // Already negative
        Assert.That(transactions[1].Amount, Is.EqualTo(100.00m)); // Positive
    }

    #endregion

    #region Date Format Tests

    [Test]
    [TestCase("2026-01-15", 2026, 1, 15)]
    [TestCase("15-01-2026", 2026, 1, 15)]
    [TestCase("01/15/2026", 2026, 1, 15)]
    [TestCase("15/01/2026", 2026, 1, 15)]
    [TestCase("20260115", 2026, 1, 15)]
    [TestCase("2026/01/15", 2026, 1, 15)]
    [TestCase("15.01.2026", 2026, 1, 15)]
    public async Task ParseAsync_VariousDateFormats_ParsesCorrectly(string dateString, int year, int month, int day)
    {
        // Arrange
        var csv = $"Date,Amount,Description\n{dateString},100.00,Test";
        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(1));
        Assert.That(transactions[0].Date, Is.EqualTo(new DateTime(year, month, day)));
    }

    [Test]
    public async Task ParseAsync_QuotedINGDateFormat_ParsesCorrectly()
    {
        // Arrange
        var csv = "Date,Amount,Description\n\"20230215\",100.00,Test";
        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(1));
        Assert.That(transactions[0].Date, Is.EqualTo(new DateTime(2023, 2, 15)));
    }

    #endregion

    #region Amount Format Tests

    [Test]
    [TestCase("100.50", 100.50)]
    [TestCase("-25.00", -25.00)]
    [TestCase("50,00", 50.00)]
    [TestCase("\"50,00\"", 50.00)]
    [TestCase("€100.00", 100.00)]
    [TestCase("  100.50  ", 100.50)]
    public async Task ParseAsync_VariousAmountFormats_ParsesCorrectly(string amountString, decimal expectedAmount)
    {
        // Arrange
        var csv = $"Date,Amount,Description\n2026-01-01,{amountString},Test";
        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(1));
        Assert.That(transactions[0].Amount, Is.EqualTo(expectedAmount));
    }

    #endregion

    #region CSV Special Characters Tests

    [Test]
    public async Task ParseAsync_DescriptionWithComma_ParsesCorrectly()
    {
        // Arrange
        var csv = "Date,Amount,Description\n" +
                  "2026-01-01,100.00,\"Test, with comma\"";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(1));
        Assert.That(transactions[0].Description, Is.EqualTo("Test, with comma"));
    }

    [Test]
    public async Task ParseAsync_DescriptionWithQuotes_ParsesCorrectly()
    {
        // Arrange
        var csv = "Date,Amount,Description\n" +
                  "2026-01-01,100.00,\"Test \"\"quoted\"\" text\"";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(1));
        Assert.That(transactions[0].Description, Is.EqualTo("Test \"quoted\" text"));
    }

    [Test]
    public async Task ParseAsync_DescriptionWithSemicolon_ParsesCorrectly()
    {
        // Arrange - semicolon in quotes shouldn't be treated as delimiter
        var csv = "Date,Amount,Description\n" +
                  "2026-01-01,100.00,\"Test; with semicolon\"";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(1));
        Assert.That(transactions[0].Description, Is.EqualTo("Test; with semicolon"));
    }

    #endregion

    #region Optional Fields Tests

    [Test]
    public async Task ParseAsync_OptionalCounterpartyAndExternalId_ParsesCorrectly()
    {
        // Arrange
        var csv = "Date,Amount,Description,Counterparty,ExternalId\n" +
                  "2026-01-01,100.00,Salary,Employer Inc,TXN001\n" +
                  "2026-01-02,-25.00,Groceries,,";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description",
            CounterpartyColumn = "Counterparty",
            ExternalIdColumn = "ExternalId"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(2));

        Assert.That(transactions[0].Counterparty, Is.EqualTo("Employer Inc"));
        Assert.That(transactions[0].ExternalId, Is.EqualTo("TXN001"));

        Assert.That(transactions[1].Counterparty, Is.Null.Or.Empty);
        Assert.That(transactions[1].ExternalId, Is.Null.Or.Empty);
    }

    [Test]
    public async Task ParseAsync_MissingDescriptionColumn_UsesDefaultValue()
    {
        // Arrange
        var csv = "Date,Amount\n2026-01-01,100.00";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(1));
        Assert.That(transactions[0].Description, Is.EqualTo("No description"));
    }

    #endregion

    #region Original Import String Tests

    [Test]
    public async Task ParseAsync_StoresOriginalImportString()
    {
        // Arrange
        var csv = "Date,Amount,Description\n2026-01-01,100.50,Test";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(1));
        Assert.That(transactions[0].OriginalImport, Is.EqualTo("2026-01-01,100.50,Test"));
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task ParseAsync_InvalidDate_SkipsRow()
    {
        // Arrange
        var csv = "Date,Amount,Description\n" +
                  "2026-01-01,100.00,Valid\n" +
                  "invalid-date,50.00,Invalid\n" +
                  "2026-01-03,75.00,Valid2";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert - should skip the invalid row
        Assert.That(transactions, Has.Count.EqualTo(2));
        Assert.That(transactions[0].Description, Is.EqualTo("Valid"));
        Assert.That(transactions[1].Description, Is.EqualTo("Valid2"));
    }

    [Test]
    public async Task ParseAsync_InvalidAmount_SkipsRow()
    {
        // Arrange
        var csv = "Date,Amount,Description\n" +
                  "2026-01-01,100.00,Valid\n" +
                  "2026-01-02,invalid,Invalid\n" +
                  "2026-01-03,75.00,Valid2";

        var mapping = new ColumnMappingDto
        {
            DateColumn = "Date",
            AmountColumn = "Amount",
            DescriptionColumn = "Description"
        };

        // Act
        var transactions = await _parser.ParseAsync(csv, mapping);

        // Assert
        Assert.That(transactions, Has.Count.EqualTo(2));
        Assert.That(transactions[0].Description, Is.EqualTo("Valid"));
        Assert.That(transactions[1].Description, Is.EqualTo("Valid2"));
    }

    #endregion
}
