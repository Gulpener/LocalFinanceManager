using LocalFinanceManager.Models;

namespace LocalFinanceManager.Tests.Unit;

[TestFixture]
public class BudgetLineTests
{
    [Test]
    public void MonthlyAmounts_Serialization_WorksCorrectly()
    {
        // Arrange
        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid()
        };

        var amounts = new decimal[] { 100m, 200m, 300m, 400m, 500m, 600m, 700m, 800m, 900m, 1000m, 1100m, 1200m };

        // Act
        budgetLine.MonthlyAmounts = amounts;
        var retrieved = budgetLine.MonthlyAmounts;

        // Assert
        Assert.That(retrieved, Is.EqualTo(amounts));
        Assert.That(retrieved.Length, Is.EqualTo(12));
    }

    [Test]
    public void YearTotal_CalculatesCorrectSum()
    {
        // Arrange
        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            MonthlyAmounts = new decimal[] { 100m, 200m, 300m, 400m, 500m, 600m, 700m, 800m, 900m, 1000m, 1100m, 1200m }
        };

        // Act
        var yearTotal = budgetLine.YearTotal;

        // Assert
        Assert.That(yearTotal, Is.EqualTo(7800m));
    }

    [Test]
    public void YearTotal_WithZeroAmounts_ReturnsZero()
    {
        // Arrange
        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            MonthlyAmounts = new decimal[12]
        };

        // Act
        var yearTotal = budgetLine.YearTotal;

        // Assert
        Assert.That(yearTotal, Is.EqualTo(0m));
    }

    [Test]
    public void YearTotal_WithUniformAmounts_CalculatesCorrectly()
    {
        // Arrange
        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            MonthlyAmounts = Enumerable.Repeat(500m, 12).ToArray()
        };

        // Act
        var yearTotal = budgetLine.YearTotal;

        // Assert
        Assert.That(yearTotal, Is.EqualTo(6000m));
    }

    [Test]
    public void MonthlyAmounts_EmptyJson_ReturnsEmptyArray()
    {
        // Arrange
        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            MonthlyAmountsJson = "[]"
        };

        // Act
        var amounts = budgetLine.MonthlyAmounts;

        // Assert
        Assert.That(amounts, Is.Empty);
    }

    [Test]
    public void MonthlyAmounts_InvalidJson_ThrowsException()
    {
        // Arrange
        var budgetLine = new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetPlanId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            MonthlyAmountsJson = "invalid"
        };

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() =>
        {
            var amounts = budgetLine.MonthlyAmounts;
        });
    }
}
