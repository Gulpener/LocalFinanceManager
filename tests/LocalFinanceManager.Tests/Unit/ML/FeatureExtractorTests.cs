using LocalFinanceManager.ML;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Unit.ML;

/// <summary>
/// Unit tests for FeatureExtractor service.
/// Tests tokenization, amount binning, temporal patterns.
/// </summary>
[TestFixture]
public class FeatureExtractorTests
{
    private FeatureExtractor _featureExtractor = null!;

    [SetUp]
    public void SetUp()
    {
        _featureExtractor = new FeatureExtractor();
    }

    [Test]
    public void ExtractFeatures_TokenizesDescription_RemovesStopWords()
    {
        // Arrange
        var transactionData = new TransactionData
        {
            Description = "Payment to the grocery store and market",
            Counterparty = "Grocery Store",
            Amount = -45.50m,
            Date = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            AccountId = Guid.NewGuid()
        };

        // Act
        var features = _featureExtractor.ExtractFeatures(transactionData);

        // Assert
        Assert.That(features.DescriptionTokens, Does.Not.Contain("the"));
        Assert.That(features.DescriptionTokens, Does.Not.Contain("to"));
        Assert.That(features.DescriptionTokens, Does.Not.Contain("and"));
        Assert.That(features.DescriptionTokens, Does.Contain("payment"));
        Assert.That(features.DescriptionTokens, Does.Contain("grocery"));
        Assert.That(features.DescriptionTokens, Does.Contain("store"));
        Assert.That(features.DescriptionTokens, Does.Contain("market"));
    }

    [Test]
    [TestCase(-5, 0)] // Micro
    [TestCase(-9.99, 0)] // Micro
    [TestCase(-10, 1)] // Small
    [TestCase(-50, 1)] // Small
    [TestCase(-99.99, 1)] // Small
    [TestCase(-100, 2)] // Medium
    [TestCase(-500, 2)] // Medium
    [TestCase(-999.99, 2)] // Medium
    [TestCase(-1000, 3)] // Large
    [TestCase(-5000, 3)] // Large
    [TestCase(-9999.99, 3)] // Large
    [TestCase(-10000, 4)] // XLarge
    [TestCase(-50000, 4)] // XLarge
    public void ExtractFeatures_BinsAmountCorrectly(decimal amount, int expectedBinValue)
    {
        // Arrange
        var transactionData = new TransactionData
        {
            Description = "Test transaction",
            Amount = amount,
            Date = DateTime.UtcNow,
            AccountId = Guid.NewGuid()
        };

        // Act
        var features = _featureExtractor.ExtractFeatures(transactionData);

        // Assert
        Assert.That(features.AmountBin, Is.EqualTo((AmountBin)expectedBinValue));
        Assert.That(features.AbsoluteAmount, Is.EqualTo(Math.Abs(amount)));
    }

    [Test]
    public void ExtractFeatures_IdentifiesIncome_WhenAmountPositive()
    {
        // Arrange
        var transactionData = new TransactionData
        {
            Description = "Salary payment",
            Amount = 2500m,
            Date = DateTime.UtcNow,
            AccountId = Guid.NewGuid()
        };

        // Act
        var features = _featureExtractor.ExtractFeatures(transactionData);

        // Assert
        Assert.That(features.IsIncome, Is.True);
    }

    [Test]
    public void ExtractFeatures_IdentifiesExpense_WhenAmountNegative()
    {
        // Arrange
        var transactionData = new TransactionData
        {
            Description = "Grocery shopping",
            Amount = -45.50m,
            Date = DateTime.UtcNow,
            AccountId = Guid.NewGuid()
        };

        // Act
        var features = _featureExtractor.ExtractFeatures(transactionData);

        // Assert
        Assert.That(features.IsIncome, Is.False);
    }

    [Test]
    public void ExtractFeatures_ExtractsTemporalPatterns()
    {
        // Arrange
        var date = new DateTime(2026, 3, 14); // March 14, 2026 (Q1, Saturday)
        var transactionData = new TransactionData
        {
            Description = "Weekend shopping",
            Amount = -75.00m,
            Date = date,
            AccountId = Guid.NewGuid()
        };

        // Act
        var features = _featureExtractor.ExtractFeatures(transactionData);

        // Assert
        Assert.That(features.DayOfWeek, Is.EqualTo((int)DayOfWeek.Saturday));
        Assert.That(features.Month, Is.EqualTo(3));
        Assert.That(features.Quarter, Is.EqualTo(1));
    }

    [Test]
    public void ExtractFeatures_QuarterCalculation_CorrectForAllMonths()
    {
        // Test Q1 (Jan-Mar)
        Assert.That(ExtractQuarter(1), Is.EqualTo(1));
        Assert.That(ExtractQuarter(2), Is.EqualTo(1));
        Assert.That(ExtractQuarter(3), Is.EqualTo(1));

        // Test Q2 (Apr-Jun)
        Assert.That(ExtractQuarter(4), Is.EqualTo(2));
        Assert.That(ExtractQuarter(5), Is.EqualTo(2));
        Assert.That(ExtractQuarter(6), Is.EqualTo(2));

        // Test Q3 (Jul-Sep)
        Assert.That(ExtractQuarter(7), Is.EqualTo(3));
        Assert.That(ExtractQuarter(8), Is.EqualTo(3));
        Assert.That(ExtractQuarter(9), Is.EqualTo(3));

        // Test Q4 (Oct-Dec)
        Assert.That(ExtractQuarter(10), Is.EqualTo(4));
        Assert.That(ExtractQuarter(11), Is.EqualTo(4));
        Assert.That(ExtractQuarter(12), Is.EqualTo(4));
    }

    [Test]
    public void ExtractFeatures_NormalizesCounterparty()
    {
        // Arrange
        var transactionData = new TransactionData
        {
            Description = "Purchase",
            Counterparty = "  Albert HEIJN  ",
            Amount = -30m,
            Date = DateTime.UtcNow,
            AccountId = Guid.NewGuid()
        };

        // Act
        var features = _featureExtractor.ExtractFeatures(transactionData);

        // Assert
        Assert.That(features.Counterparty, Is.EqualTo("albert heijn"));
    }

    [Test]
    public void ExtractFeatures_HandlesNullCounterparty()
    {
        // Arrange
        var transactionData = new TransactionData
        {
            Description = "Cash withdrawal",
            Counterparty = null,
            Amount = -50m,
            Date = DateTime.UtcNow,
            AccountId = Guid.NewGuid()
        };

        // Act
        var features = _featureExtractor.ExtractFeatures(transactionData);

        // Assert
        Assert.That(features.Counterparty, Is.Null);
    }

    [Test]
    public void ExtractFeatures_RemovesPunctuation_FromTokenization()
    {
        // Arrange
        var transactionData = new TransactionData
        {
            Description = "Payment: GROCERY-STORE (Market)",
            Amount = -40m,
            Date = DateTime.UtcNow,
            AccountId = Guid.NewGuid()
        };

        // Act
        var features = _featureExtractor.ExtractFeatures(transactionData);

        // Assert
        Assert.That(features.DescriptionTokens, Does.Contain("payment"));
        Assert.That(features.DescriptionTokens, Does.Contain("grocery"));
        Assert.That(features.DescriptionTokens, Does.Contain("store"));
        Assert.That(features.DescriptionTokens, Does.Contain("market"));
        // Punctuation removed
        Assert.That(features.DescriptionTokens, Does.Not.Contain(":"));
        Assert.That(features.DescriptionTokens, Does.Not.Contain("-"));
        Assert.That(features.DescriptionTokens, Does.Not.Contain("("));
    }

    [Test]
    public void ToMLInput_ConvertsFeaturesToMLFormat()
    {
        // Arrange
        var features = new TransactionFeatures
        {
            DescriptionTokens = new[] { "grocery", "store" },
            Counterparty = "albert heijn",
            AmountBin = AmountBin.Small,
            DayOfWeek = 3,
            Month = 6,
            Quarter = 2,
            AccountId = Guid.NewGuid(),
            AbsoluteAmount = 45.50m,
            IsIncome = false
        };
        var categoryId = Guid.NewGuid();

        // Act
        var mlInput = _featureExtractor.ToMLInput(features, categoryId);

        // Assert
        Assert.That(mlInput.DescriptionText, Is.EqualTo("grocery store"));
        Assert.That(mlInput.Counterparty, Is.EqualTo("albert heijn"));
        Assert.That(mlInput.AmountBin, Is.EqualTo((float)AmountBin.Small));
        Assert.That(mlInput.DayOfWeek, Is.EqualTo(3f));
        Assert.That(mlInput.Month, Is.EqualTo(6f));
        Assert.That(mlInput.Quarter, Is.EqualTo(2f));
        Assert.That(mlInput.AbsoluteAmount, Is.EqualTo(45.50f));
        Assert.That(mlInput.IsIncome, Is.EqualTo(0f)); // false = 0
        Assert.That(mlInput.CategoryId, Is.EqualTo(categoryId.ToString()));
    }

    private int ExtractQuarter(int month)
    {
        var transactionData = new TransactionData
        {
            Description = "Test",
            Amount = -10m,
            Date = new DateTime(2026, month, 1),
            AccountId = Guid.NewGuid()
        };
        var features = _featureExtractor.ExtractFeatures(transactionData);
        return features.Quarter;
    }
}
