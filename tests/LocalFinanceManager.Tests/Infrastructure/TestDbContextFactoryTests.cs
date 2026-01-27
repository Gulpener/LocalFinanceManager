using LocalFinanceManager.Tests.Fixtures;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Tests.Infrastructure;

/// <summary>
/// Tests for TestDbContextFactory to ensure in-memory SQLite contexts are created correctly.
/// </summary>
[TestFixture]
public class TestDbContextFactoryTests
{
    [Test]
    public void CreateContext_ShouldReturnValidContext()
    {
        // Arrange & Act
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        // Assert
        Assert.That(context, Is.Not.Null);
        Assert.That(context.Database.CanConnect(), Is.True);
    }

    [Test]
    public async Task CreateContext_ShouldAllowAddingEntities()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var model = new MLModel
        {
            Id = Guid.NewGuid(),
            ModelType = "TestModel",
            Version = 1,
            TrainedAt = DateTime.UtcNow,
            ModelBytes = new byte[] { 0x01, 0x02 },
            Metrics = "{}"
        };

        // Act
        context.MLModels.Add(model);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.MLModels.FindAsync(model.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ModelType, Is.EqualTo("TestModel"));
    }

    [Test]
    public async Task MultipleContexts_ShouldBeIsolated()
    {
        // Arrange
        using var factory1 = new TestDbContextFactory();
        using var factory2 = new TestDbContextFactory();
        using var context1 = factory1.CreateContext();
        using var context2 = factory2.CreateContext();

        var model = TestDataBuilder.CreateMLModel();

        // Act
        context1.MLModels.Add(model);
        await context1.SaveChangesAsync();

        var count1 = await context1.MLModels.CountAsync();
        var count2 = await context2.MLModels.CountAsync();

        // Assert
        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(0), "Contexts should be isolated");
    }
}
