using LocalFinanceManager.ML;

namespace LocalFinanceManager.ML.Tests;

[TestFixture]
public class FeatureExtractorTests
{
    [Test]
    public void ExtractFeatures_MapsExpectedFields()
    {
        var extractor = new FeatureExtractor();
        var accountId = Guid.NewGuid();
        var transaction = new TransactionData
        {
            Description = "Payment to Grocery Store",
            Counterparty = "  Albert Heijn  ",
            Amount = -42.50m,
            Date = new DateTime(2026, 3, 1),
            AccountId = accountId
        };

        var features = extractor.ExtractFeatures(transaction);

        Assert.Multiple(() =>
        {
            Assert.That(features.DescriptionTokens, Does.Contain("payment"));
            Assert.That(features.DescriptionTokens, Does.Contain("grocery"));
            Assert.That(features.DescriptionTokens, Does.Contain("store"));
            Assert.That(features.Counterparty, Is.EqualTo("albert heijn"));
            Assert.That(features.AmountBin, Is.EqualTo(AmountBin.Small));
            Assert.That(features.Month, Is.EqualTo(3));
            Assert.That(features.Quarter, Is.EqualTo(1));
            Assert.That(features.AbsoluteAmount, Is.EqualTo(42.50m));
            Assert.That(features.IsIncome, Is.False);
            Assert.That(features.AccountId, Is.EqualTo(accountId));
        });
    }

    [Test]
    public void ToMLInput_MapsExtractedFeaturesToPredictionInput()
    {
        var extractor = new FeatureExtractor();
        var categoryId = Guid.NewGuid();
        var features = new TransactionFeatures
        {
            DescriptionTokens = new[] { "online", "subscription" },
            Counterparty = "streaming inc",
            AmountBin = AmountBin.Medium,
            DayOfWeek = 2,
            Month = 10,
            Quarter = 4,
            AbsoluteAmount = 125.99m,
            IsIncome = true
        };

        var input = extractor.ToMLInput(features, categoryId);

        Assert.Multiple(() =>
        {
            Assert.That(input.DescriptionText, Is.EqualTo("online subscription"));
            Assert.That(input.Counterparty, Is.EqualTo("streaming inc"));
            Assert.That(input.AmountBin, Is.EqualTo((float)AmountBin.Medium));
            Assert.That(input.DayOfWeek, Is.EqualTo(2f));
            Assert.That(input.Month, Is.EqualTo(10f));
            Assert.That(input.Quarter, Is.EqualTo(4f));
            Assert.That(input.AbsoluteAmount, Is.EqualTo(125.99f).Within(0.001f));
            Assert.That(input.IsIncome, Is.EqualTo(1.0f));
            Assert.That(input.CategoryId, Is.EqualTo(categoryId.ToString()));
        });
    }
}
