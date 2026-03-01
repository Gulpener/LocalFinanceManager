using Bunit;
using Bunit.JSInterop;
using LocalFinanceManager.Components.Pages;
using LocalFinanceManager.Components.Shared;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace LocalFinanceManager.Tests.Components;

[TestFixture]
public class ShortcutHelpTests
{
    [Test]
    public void HelpModal_Displays_AllKeyboardShortcuts()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

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
        context.JSInterop.Mode = JSRuntimeMode.Loose;

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
        context.JSInterop.Mode = JSRuntimeMode.Loose;

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
        context.JSInterop.Mode = JSRuntimeMode.Loose;

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
        context.JSInterop.Mode = JSRuntimeMode.Loose;

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

    [Test]
    public async Task HelpModal_ReleasesFocusTrap_WhenVisibilityTurnsFalse()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = context.Render<ShortcutHelp>(parameters => parameters
            .Add(p => p.IsVisible, true));

        cut.WaitForAssertion(() =>
            Assert.That(context.JSInterop.Invocations.Any(i => i.Identifier == "localFinanceKeyboard.trapFocus"), Is.True));

        await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(ShortcutHelp.IsVisible)] = false
        })));
        Assert.That(
            context.JSInterop.Invocations.Any(i => i.Identifier == "localFinanceKeyboard.releaseFocusTrap"),
            Is.True,
            "Expected ShortcutHelp to release the document focus trap when hidden.");
    }

    [Test]
    public async Task HelpModal_ReleasesFocusTrap_OnDispose_WhenVisible()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = context.Render<ShortcutHelp>(parameters => parameters
            .Add(p => p.IsVisible, true));

        cut.WaitForAssertion(() =>
            Assert.That(context.JSInterop.Invocations.Any(i => i.Identifier == "localFinanceKeyboard.trapFocus"), Is.True));

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        Assert.That(
            context.JSInterop.Invocations.Any(i => i.Identifier == "localFinanceKeyboard.releaseFocusTrap"),
            Is.True,
            "Expected ShortcutHelp disposal to release the document focus trap.");
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
        var hostEnvironmentMock = new Mock<IWebHostEnvironment>();

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

        hostEnvironmentMock
            .Setup(x => x.EnvironmentName)
            .Returns(Environments.Development);

        services.AddSingleton(transactionRepositoryMock.Object);
        services.AddSingleton(accountRepositoryMock.Object);
        services.AddSingleton(deviceDetectionServiceMock.Object);
        services.AddSingleton(filterStateServiceMock.Object);
        services.AddSingleton(assignmentServiceMock.Object);
        services.AddSingleton(budgetLineRepositoryMock.Object);
        services.AddSingleton(budgetPlanRepositoryMock.Object);
        services.AddSingleton(recentCategoriesServiceMock.Object);
        services.AddSingleton(hostEnvironmentMock.Object);
        services.AddLogging();
    }
}
