using Bunit;
using Bunit.JSInterop;
using LocalFinanceManager.Components.Shared;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LocalFinanceManager.Tests.Components;

[TestFixture]
public class TransactionAssignModalFocusTrapTests
{
    [TestCase(2026)]
    [TestCase(2024)]
    public void TransactionAssignModal_RendersResolvedYearInTitle(int year)
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var assignmentServiceMock = new Mock<ITransactionAssignmentService>();
        var budgetPlanRepositoryMock = new Mock<IBudgetPlanRepository>();
        var budgetLineRepositoryMock = new Mock<IBudgetLineRepository>();
        var recentCategoriesServiceMock = new Mock<IRecentCategoriesService>();

        var accountId = Guid.NewGuid();
        budgetPlanRepositoryMock
            .Setup(x => x.GetByAccountAndYearAsync(accountId, year))
            .ReturnsAsync(new BudgetPlan
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Year = year,
                Name = $"Budget {year}"
            });

        budgetLineRepositoryMock
            .Setup(x => x.GetByBudgetPlanIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<BudgetLine>());

        recentCategoriesServiceMock
            .Setup(x => x.GetRecentCategoriesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Guid>());

        context.Services.AddSingleton(assignmentServiceMock.Object);
        context.Services.AddSingleton(budgetPlanRepositoryMock.Object);
        context.Services.AddSingleton(budgetLineRepositoryMock.Object);
        context.Services.AddSingleton(recentCategoriesServiceMock.Object);

        var transaction = new TransactionDto
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = new DateTime(year, 1, 15),
            Amount = -50m,
            Description = "Test transactie",
            XMin = 1u
        };

        var cut = context.Render<TransactionAssignModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, transaction)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnAssignmentSuccess, EventCallback.Factory.Create(this, () => { })));

        var titleText = cut.Find("#assignModalTitle").TextContent.Trim();

        Assert.That(titleText, Is.EqualTo($"Transactie toewijzen ({year})"));
        Assert.That(titleText, Does.Not.Contain("@TransactionYearSuffix"));
    }

    [Test]
    public async Task TransactionAssignModal_ReleasesFocusTrap_OnDispose_WhenVisible()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var assignmentServiceMock = new Mock<ITransactionAssignmentService>();
        var budgetPlanRepositoryMock = new Mock<IBudgetPlanRepository>();
        var budgetLineRepositoryMock = new Mock<IBudgetLineRepository>();
        var recentCategoriesServiceMock = new Mock<IRecentCategoriesService>();

        var accountId = Guid.NewGuid();
        budgetPlanRepositoryMock
            .Setup(x => x.GetByAccountAndYearAsync(accountId, 2026))
            .ReturnsAsync(new BudgetPlan
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Year = 2026,
                Name = "Test BudgetPlan"
            });

        budgetLineRepositoryMock
            .Setup(x => x.GetByBudgetPlanIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<BudgetLine>());

        recentCategoriesServiceMock
            .Setup(x => x.GetRecentCategoriesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Guid>());

        context.Services.AddSingleton(assignmentServiceMock.Object);
        context.Services.AddSingleton(budgetPlanRepositoryMock.Object);
        context.Services.AddSingleton(budgetLineRepositoryMock.Object);
        context.Services.AddSingleton(recentCategoriesServiceMock.Object);

        var transaction = new TransactionDto
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = new DateTime(2026, 1, 15),
            Amount = -50m,
            Description = "Test transactie",
            XMin = 1u
        };

        var cut = context.Render<TransactionAssignModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, transaction)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnAssignmentSuccess, EventCallback.Factory.Create(this, () => { })));

        cut.WaitForAssertion(() =>
            Assert.That(context.JSInterop.Invocations.Any(i => i.Identifier == "localFinanceKeyboard.trapFocus"), Is.True));

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        Assert.That(
            context.JSInterop.Invocations.Any(i => i.Identifier == "localFinanceKeyboard.releaseFocusTrap"),
            Is.True,
            "Expected TransactionAssignModal disposal to release the document focus trap.");
    }
}
