using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Infrastructure.Import;

namespace Infrastructure.Tests;

public class CsvTransactionImporterTests
{
    private readonly CsvTransactionImporter _importer;
    private readonly ImportConfiguration _config;

    public CsvTransactionImporterTests()
    {
        _importer = new CsvTransactionImporter();
        _config = new ImportConfiguration
        {
            AccountId = 1,
            DefaultCategoryId = 1,
            Delimiter = ',',
            HasHeader = true
        };
    }

    [Fact]
    public void CanHandle_ShouldReturnTrue_ForCsvFiles()
    {
        Assert.True(_importer.CanHandle("transactions.csv"));
        Assert.True(_importer.CanHandle("data.CSV"));
        Assert.True(_importer.CanHandle("transactions.tsv"));
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_ForOtherFiles()
    {
        Assert.False(_importer.CanHandle("transactions.json"));
        Assert.False(_importer.CanHandle("data.xlsx"));
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldParseValidCsv()
    {
        // Arrange
        var csv = @"Date,Amount,Description,CounterAccount,AccountNumber
2025-01-15,100.50,Albert Heijn supermarket,NL01RABO0123456789,NL99INGB9876543210
2025-01-16,-45.00,Shell gas station,NL02SHELL0011223344,NL99INGB9876543210";

        // Act
        var result = await _importer.ImportFromStringAsync(csv, _config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.SuccessCount);

        var first = result.Transactions[0];
        Assert.Equal(new DateTime(2025, 1, 15), first.Date);
        Assert.Equal(100.50m, first.Amount);
        Assert.Equal("Albert Heijn supermarket", first.Description);
        Assert.Equal("NL01RABO0123456789", first.CounterAccount);
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldPreserveOriginalCsv()
    {
        // Arrange
        var csv = @"Date,Amount,Description,CounterAccount,AccountNumber
2025-01-15,100.50,Test Transaction,NL01TEST0000000001,NL99INGB9876543210";

        // Act
        var result = await _importer.ImportFromStringAsync(csv, _config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("100.50", result.Transactions[0].OriginalCsv);
        Assert.Contains("Test Transaction", result.Transactions[0].OriginalCsv);
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldHandleQuotedFields()
    {
        // Arrange
        var csv = @"Date,Amount,Description,CounterAccount,AccountNumber
2025-01-15,50.00,""Description with, comma"",NL01TEST0000000001,NL99INGB9876543210";

        // Act
        var result = await _importer.ImportFromStringAsync(csv, _config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Description with, comma", result.Transactions[0].Description);
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldHandleNegativeAmounts()
    {
        // Arrange
        var csv = @"Date,Amount,Description,CounterAccount,AccountNumber
2025-01-16,-45.00,Expense,NL01TEST0000000001,NL99INGB9876543210";

        // Act
        var result = await _importer.ImportFromStringAsync(csv, _config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(-45.00m, result.Transactions[0].Amount);
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldReturnErrorForEmptyFile()
    {
        // Arrange
        var csv = "";

        // Act
        var result = await _importer.ImportFromStringAsync(csv, _config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("File is empty", result.Errors);
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldInferColumnMappingFromHeaders()
    {
        // Arrange - Dutch header names
        var csv = @"Datum,Bedrag,Omschrijving,Tegenrekening,Rekening
2025-01-15,100.50,Test Transaction,NL01TEST0000000001,NL99INGB9876543210";

        // Act
        var result = await _importer.ImportFromStringAsync(csv, _config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Test Transaction", result.Transactions[0].Description);
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldUseSemicolonDelimiter()
    {
        // Arrange
        var config = new ImportConfiguration
        {
            AccountId = 1,
            DefaultCategoryId = 1,
            Delimiter = ';',
            HasHeader = true
        };

        var csv = @"Date;Amount;Description;CounterAccount;AccountNumber
2025-01-15;100.50;Test Transaction;NL01TEST0000000001;NL99INGB9876543210";

        // Act
        var result = await _importer.ImportFromStringAsync(csv, config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100.50m, result.Transactions[0].Amount);
    }
}
