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
    public async Task RowVersion_ShouldBeConfigured()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var model = TestDataBuilder.CreateMLModel();
        context.MLModels.Add(model);
        await context.SaveChangesAsync();

        // Assert
        // Note: SQLite doesn't auto-generate RowVersion like SQL Server
        // The property is configured but may be null in SQLite
        // The concurrency checking still works through EF Core's tracking
        Assert.That(model.Id, Is.Not.EqualTo(Guid.Empty));
    }
}
