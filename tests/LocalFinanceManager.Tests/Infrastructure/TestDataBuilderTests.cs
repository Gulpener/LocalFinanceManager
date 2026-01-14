using LocalFinanceManager.Tests.Fixtures;

namespace LocalFinanceManager.Tests.Infrastructure;

/// <summary>
/// Tests for TestDataBuilder to ensure seed data is created correctly.
/// </summary>
[TestFixture]
public class TestDataBuilderTests
{
    [Test]
    public void CreateMLModel_ShouldReturnValidModel()
    {
        // Act
        var model = TestDataBuilder.CreateMLModel();

        // Assert
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(model.ModelType, Is.EqualTo("TestModel"));
        Assert.That(model.Version, Is.EqualTo(1));
        Assert.That(model.ModelBytes, Is.Not.Null);
        Assert.That(model.IsArchived, Is.False);
    }

    [Test]
    public void CreateMLModels_ShouldReturnRequestedCount()
    {
        // Act
        var models = TestDataBuilder.CreateMLModels(5);

        // Assert
        Assert.That(models.Count, Is.EqualTo(5));
        var uniqueIds = models.Select(m => m.Id).Distinct().Count();
        Assert.That(uniqueIds, Is.EqualTo(5), "All IDs should be unique");
    }

    [Test]
    public void CreateMLModel_WithCustomType_ShouldUseProvidedType()
    {
        // Act
        var model = TestDataBuilder.CreateMLModel("CategoryClassifier");

        // Assert
        Assert.That(model.ModelType, Is.EqualTo("CategoryClassifier"));
    }
}
