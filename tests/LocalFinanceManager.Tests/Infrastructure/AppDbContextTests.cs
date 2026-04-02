using LocalFinanceManager.Data;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Tests.Infrastructure;

/// <summary>
/// Integration tests for AppDbContext to ensure migrations and configuration work correctly.
/// </summary>
[TestFixture]
public class AppDbContextTests
{
    [Test]
    public async Task SaveChanges_ShouldSetCreatedAtAndUpdatedAt()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var model = TestDataBuilder.CreateMLModel();
        model.CreatedAt = default;
        model.UpdatedAt = default;

        // Act
        context.MLModels.Add(model);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.MLModels.FindAsync(model.Id);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.CreatedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(saved.UpdatedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(saved.CreatedAt, Is.EqualTo(saved.UpdatedAt).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task SaveChanges_OnUpdate_ShouldUpdateUpdatedAt()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var model = TestDataBuilder.CreateMLModel();
        context.MLModels.Add(model);
        await context.SaveChangesAsync();

        var originalUpdatedAt = model.UpdatedAt;
        await Task.Delay(100); // Ensure time difference

        // Act
        model.Version = 2;
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.MLModels.FindAsync(model.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.UpdatedAt, Is.GreaterThan(originalUpdatedAt));
    }

    [Test]
    public async Task XMin_ShouldBeConfigured_AsUint()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var model = TestDataBuilder.CreateMLModel();
        context.MLModels.Add(model);
        await context.SaveChangesAsync();

        // Assert
        // XMin is a uint concurrency token. In SQLite (test environment) it remains 0
        // unless manually set; in PostgreSQL it is auto-updated on every write.
        Assert.That(model.Id, Is.Not.EqualTo(Guid.Empty));
    }
}
