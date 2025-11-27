using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Infrastructure.Import;

namespace Infrastructure.Tests;

public class JsonTransactionImporterTests
{
    private readonly JsonTransactionImporter _importer;
    private readonly ImportConfiguration _config;

    public JsonTransactionImporterTests()
    {
        _importer = new JsonTransactionImporter();
        _config = new ImportConfiguration
        {
            AccountId = 1,
            DefaultCategoryId = 1
        };
    }

    [Fact]
    public void CanHandle_ShouldReturnTrue_ForJsonFiles()
    {
        Assert.True(_importer.CanHandle("transactions.json"));
        Assert.True(_importer.CanHandle("data.JSON"));
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_ForOtherFiles()
    {
        Assert.False(_importer.CanHandle("transactions.csv"));
        Assert.False(_importer.CanHandle("data.xlsx"));
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldParseValidJson()
    {
        // Arrange
        var json = @"[
            {
                ""date"": ""2025-01-15T00:00:00"",
                ""amount"": 100.50,
                ""description"": ""Albert Heijn supermarket"",
                ""counterAccount"": ""NL01RABO0123456789""
            },
            {
                ""date"": ""2025-01-16T00:00:00"",
                ""amount"": -45.00,
                ""description"": ""Shell gas station"",
                ""counterAccount"": ""NL02SHELL0011223344""
            }
        ]";

        // Act
        var result = await _importer.ImportFromStringAsync(json, _config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.SuccessCount);

        var first = result.Transactions[0];
        Assert.Equal(new DateTime(2025, 1, 15), first.Date);
        Assert.Equal(100.50m, first.Amount);
        Assert.Equal("Albert Heijn supermarket", first.Description);
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldPreserveOriginalAsJson()
    {
        // Arrange
        var json = @"[
            {
                ""date"": ""2025-01-15T00:00:00"",
                ""amount"": 100.50,
                ""description"": ""Test Transaction"",
                ""counterAccount"": ""NL01TEST0000000001""
            }
        ]";

        // Act
        var result = await _importer.ImportFromStringAsync(json, _config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Test Transaction", result.Transactions[0].OriginalCsv);
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldReturnErrorForInvalidJson()
    {
        // Arrange
        var invalidJson = "not valid json [";

        // Act
        var result = await _importer.ImportFromStringAsync(invalidJson, _config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Invalid JSON format"));
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldReturnErrorForEmptyArray()
    {
        // Arrange
        var json = "[]";

        // Act
        var result = await _importer.ImportFromStringAsync(json, _config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("No transactions found"));
    }

    [Fact]
    public async Task ImportFromStringAsync_ShouldHandleCaseInsensitiveProperties()
    {
        // Arrange
        var json = @"[
            {
                ""Date"": ""2025-01-15T00:00:00"",
                ""AMOUNT"": 100.50,
                ""Description"": ""Test"",
                ""CounterAccount"": ""NL01TEST""
            }
        ]";

        // Act
        var result = await _importer.ImportFromStringAsync(json, _config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100.50m, result.Transactions[0].Amount);
    }
}
