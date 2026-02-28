using Deque.AxeCore.Playwright;
using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Tests;

[TestFixture]
public class AccessibilityTests : E2ETestBase
{
    [Test]
    public async Task TransactionsPage_HasNoCriticalOrSeriousAxeViolations()
    {
        await Page.GotoAsync($"{BaseUrl}/transactions", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var axeResult = await Page.RunAxe();
        var severeViolations = axeResult.Violations
            .Where(v => IsCriticalOrSerious(v.Impact?.ToString()))
            .Where(v => !IsKnownNonApplicationViolation(v.ToString()))
            .ToList();

        Assert.That(severeViolations, Is.Empty, FormatViolations(severeViolations));
    }

    [Test]
    public async Task AssignmentModal_HasNoCriticalOrSeriousAxeViolations()
    {
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(context, "A11y Modal", "NL91ABNA0417164300", 1000m);
        var budgetPlanId = account.CurrentBudgetPlanId!.Value;
        var categories = await SeedDataHelper.SeedCategoriesAsync(context, budgetPlanId, "A11y Category");
        await SeedDataHelper.SeedBudgetLineAsync(context, budgetPlanId, categories[0].Id, 500m);
        await SeedDataHelper.SeedTransactionAsync(context, account.Id, -15m, DateTime.Now, "A11y Tx");

        await Page.GotoAsync($"{BaseUrl}/transactions", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.Locator("button:has-text('Toewijzen')").First.ClickAsync();
        await Expect(Page.Locator("#transactionAssignModal")).ToBeVisibleAsync();

        var axeResult = await Page.RunAxe();
        var severeViolations = axeResult.Violations
            .Where(v => IsCriticalOrSerious(v.Impact?.ToString()))
            .Where(v => !IsKnownNonApplicationViolation(v.ToString()))
            .ToList();

        Assert.That(severeViolations, Is.Empty, FormatViolations(severeViolations));
    }

    [Test]
    public async Task ShortcutHelpModal_HasNoCriticalOrSeriousAxeViolations()
    {
        await Page.GotoAsync($"{BaseUrl}/transactions", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.Locator("#shortcutHelpButton").ClickAsync();
        await Expect(Page.Locator("#shortcutHelpModal")).ToBeVisibleAsync();

        var axeResult = await Page.RunAxe();
        var severeViolations = axeResult.Violations
            .Where(v => IsCriticalOrSerious(v.Impact?.ToString()))
            .Where(v => !IsKnownNonApplicationViolation(v.ToString()))
            .ToList();

        Assert.That(severeViolations, Is.Empty, FormatViolations(severeViolations));
    }

    [Test]
    public async Task AssignmentModal_TabOrder_StartsAtExpectedFields()
    {
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(context, "Tab Order", "NL91ABNA0417164300", 1000m);
        var budgetPlanId = account.CurrentBudgetPlanId!.Value;
        var categories = await SeedDataHelper.SeedCategoriesAsync(context, budgetPlanId, "Tab Order Category");
        await SeedDataHelper.SeedBudgetLineAsync(context, budgetPlanId, categories[0].Id, 500m);
        await SeedDataHelper.SeedTransactionAsync(context, account.Id, -33m, DateTime.Now, "Tab Order Tx");

        await Page.GotoAsync($"{BaseUrl}/transactions", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.Locator("button:has-text('Toewijzen')").First.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.activeElement?.id === 'budgetLineSelect'");

        var sequence = new List<string>();
        sequence.Add(await Page.EvaluateAsync<string>("() => document.activeElement?.id || ''"));

        for (var i = 0; i < 3; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            sequence.Add(await Page.EvaluateAsync<string>("() => document.activeElement?.id || ''"));
        }

        Assert.That(sequence[0], Is.EqualTo("budgetLineSelect"));
        var focusStillInModal = await Page.EvaluateAsync<bool>("() => !!document.activeElement?.closest('#transactionAssignModal')");
        Assert.That(focusStillInModal, Is.True);
    }

    [Test]
    [Ignore("Manual verification required: Use Tab navigation and visually confirm 2px high-contrast focus outlines.")]
    public async Task FocusIndicators_ManualVerification_Recorded()
    {
        await Page.GotoAsync($"{BaseUrl}/transactions", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        TestContext.Out.WriteLine("Manual verification required: Use Tab navigation and visually confirm 2px high-contrast focus outlines.");
    }

    private static bool IsCriticalOrSerious(string? impact)
    {
        return string.Equals(impact, "critical", StringComparison.OrdinalIgnoreCase)
            || string.Equals(impact, "serious", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKnownNonApplicationViolation(string? violationText)
    {
        if (string.IsNullOrWhiteSpace(violationText))
        {
            return false;
        }

        return violationText.Contains("\"id\": \"tabindex\"", StringComparison.OrdinalIgnoreCase)
            && violationText.Contains("\"target\": \"#stack\"", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatViolations<T>(IReadOnlyCollection<T> violations)
    {
        if (violations.Count == 0)
        {
            return string.Empty;
        }

        return "Axe violations: " + string.Join(" | ", violations.Select(v => v?.ToString()));
    }
}
