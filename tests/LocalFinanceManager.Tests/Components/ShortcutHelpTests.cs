using Bunit;
using LocalFinanceManager.Components.Pages;
using LocalFinanceManager.Components.Shared;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LocalFinanceManager.Tests.Components;

[TestFixture]
public class ShortcutHelpTests
{
    [Test]
    public void HelpModal_Displays_AllKeyboardShortcuts()
    {
        using var context = new BunitContext();
        var cut = context.Render<ShortcutHelp>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsTouchDevice, false));

        var text = cut.Markup;

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Tab"));
            Assert.That(text, Does.Contain("Enter"));
            Assert.That(text, Does.Contain("Esc"));
            Assert.That(text, Does.Contain("Space"));
            Assert.That(text, Does.Contain("Ctrl"));
            Assert.That(text, Does.Contain("/"));
            Assert.That(text, Does.Contain("?"));
            Assert.That(text, Does.Contain("↑"));
            Assert.That(text, Does.Contain("Home"));
            Assert.That(text, Does.Contain("End"));
        });
    }

    [Test]
    public void HelpModal_Closes_When_EscapePressed()
    {
        using var context = new BunitContext();
        var closed = false;

        var cut = context.Render<ShortcutHelp>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closed = true)));

        cut.Find("#shortcutHelpModal").KeyDown("Escape");

        Assert.That(closed, Is.True);
    }

    [Test]
    public void HelpModal_Shows_TouchSection_OnTouchDevices()
    {
        using var context = new BunitContext();
        var cut = context.Render<ShortcutHelp>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsTouchDevice, true));

        var text = cut.Markup;

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Touch gebaren"));
            Assert.That(text, Does.Contain("Tik"));
            Assert.That(text, Does.Contain("Swipe"));
            Assert.That(text, Does.Contain("Lang indrukken"));
        });
    }

    [Test]
    public async Task Transactions_HandleGlobalShortcut_QuestionMark_ShowsShortcutHelpModal()
    {
        using var context = new BunitContext();

        var cut = RenderTransactions(context);

        Assert.That(cut.FindAll("#shortcutHelpModal"), Is.Empty);

        await cut.InvokeAsync(() => cut.Instance.HandleGlobalShortcut("?"));

        cut.WaitForAssertion(() =>
        {
            var modal = cut.Find("#shortcutHelpModal");
            Assert.That(modal, Is.Not.Null);
        });
    }

    [Test]
    public async Task Transactions_HandleGlobalShortcut_Escape_ClosesShortcutHelpModal()
    {
        using var context = new BunitContext();

        var cut = RenderTransactions(context);

        await cut.InvokeAsync(() => cut.Instance.HandleGlobalShortcut("?"));

        cut.WaitForAssertion(() =>
        {
            var modal = cut.Find("#shortcutHelpModal");
            Assert.That(modal, Is.Not.Null);
        });

        await cut.InvokeAsync(() => cut.Instance.HandleGlobalShortcut("Escape"));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll("#shortcutHelpModal"), Is.Empty);
        });
    }

    private static IRenderedComponent<Transactions> RenderTransactions(BunitContext context)
    {
        ConfigureTransactionsDependencies(context.Services);
        return context.Render<Transactions>();
    }

    private static void ConfigureTransactionsDependencies(IServiceCollection services)
    {
        var transactionRepositoryMock = new Mock<ITransactionRepository>();
        var accountRepositoryMock = new Mock<IAccountRepository>();
        var deviceDetectionServiceMock = new Mock<IDeviceDetectionService>();
        var filterStateServiceMock = new Mock<IFilterStateService>();
        var assignmentServiceMock = new Mock<ITransactionAssignmentService>();
        var budgetLineRepositoryMock = new Mock<IBudgetLineRepository>();
        var budgetPlanRepositoryMock = new Mock<IBudgetPlanRepository>();
        var recentCategoriesServiceMock = new Mock<IRecentCategoriesService>();

        transactionRepositoryMock
            .Setup(x => x.GetAllWithSplitsAsync())
            .ReturnsAsync(new List<Transaction>());

        accountRepositoryMock
            .Setup(x => x.GetActiveAsync())
            .ReturnsAsync(new List<Account>());

        deviceDetectionServiceMock
            .Setup(x => x.IsTouchDeviceAsync())
            .ReturnsAsync(true);

        deviceDetectionServiceMock
            .Setup(x => x.GetOperatingSystemAsync())
            .ReturnsAsync(ClientOperatingSystem.Windows);

        filterStateServiceMock
            .Setup(x => x.LoadFiltersAsync())
            .ReturnsAsync((FilterState?)null);

        services.AddSingleton(transactionRepositoryMock.Object);
        services.AddSingleton(accountRepositoryMock.Object);
        services.AddSingleton(deviceDetectionServiceMock.Object);
        services.AddSingleton(filterStateServiceMock.Object);
        services.AddSingleton(assignmentServiceMock.Object);
        services.AddSingleton(budgetLineRepositoryMock.Object);
        services.AddSingleton(budgetPlanRepositoryMock.Object);
        services.AddSingleton(recentCategoriesServiceMock.Object);
        services.AddLogging();
    }
}
