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
public class SplitEditorFocusTrapTests
{
    [Test]
    public async Task SplitEditor_ReleasesFocusTrap_WhenVisibilityTurnsFalse()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var assignmentServiceMock = new Mock<ITransactionAssignmentService>();
        var budgetPlanRepositoryMock = new Mock<IBudgetPlanRepository>();
        var budgetLineRepositoryMock = new Mock<IBudgetLineRepository>();

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

        context.Services.AddSingleton(assignmentServiceMock.Object);
        context.Services.AddSingleton(budgetPlanRepositoryMock.Object);
        context.Services.AddSingleton(budgetLineRepositoryMock.Object);

        var transaction = new TransactionDto
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = new DateTime(2026, 1, 15),
            Amount = -50m,
            Description = "Test transactie",
            RowVersion = new byte[] { 1 }
        };

        var cut = context.Render<SplitEditor>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, transaction)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnSplitSaved, EventCallback.Factory.Create(this, () => { })));

        cut.WaitForAssertion(() =>
            Assert.That(context.JSInterop.Invocations.Any(i => i.Identifier == "localFinanceKeyboard.trapFocus"), Is.True));

        await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(SplitEditor.IsVisible)] = false
        })));
        Assert.That(
            context.JSInterop.Invocations.Any(i => i.Identifier == "localFinanceKeyboard.releaseFocusTrap"),
            Is.True,
            "Expected SplitEditor to release the document focus trap when hidden.");
    }

    [Test]
    public async Task SplitEditor_ReleasesFocusTrap_OnDispose_WhenVisible()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var assignmentServiceMock = new Mock<ITransactionAssignmentService>();
        var budgetPlanRepositoryMock = new Mock<IBudgetPlanRepository>();
        var budgetLineRepositoryMock = new Mock<IBudgetLineRepository>();

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

        context.Services.AddSingleton(assignmentServiceMock.Object);
        context.Services.AddSingleton(budgetPlanRepositoryMock.Object);
        context.Services.AddSingleton(budgetLineRepositoryMock.Object);

        var transaction = new TransactionDto
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = new DateTime(2026, 1, 15),
            Amount = -50m,
            Description = "Test transactie",
            RowVersion = new byte[] { 1 }
        };

        var cut = context.Render<SplitEditor>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, transaction)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnSplitSaved, EventCallback.Factory.Create(this, () => { })));

        cut.WaitForAssertion(() =>
            Assert.That(context.JSInterop.Invocations.Any(i => i.Identifier == "localFinanceKeyboard.trapFocus"), Is.True));

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        Assert.That(
            context.JSInterop.Invocations.Any(i => i.Identifier == "localFinanceKeyboard.releaseFocusTrap"),
            Is.True,
            "Expected SplitEditor disposal to release the document focus trap.");
    }

}
