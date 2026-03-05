using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using LocalFinanceManager.ML;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LocalFinanceManager.E2E.ML;

/// <summary>
/// E2E tests for ML suggestion badge and manual suggestion acceptance.
/// Tests UserStory-8 Section 15: ML suggestion display, tooltips, accept/reject, filtering, sorting.
/// </summary>
[TestFixture]
public class MLSuggestionTests : E2ETestBase
{
    private TransactionsPageModel _transactionsPage = null!;
    private Account _testAccount = null!;
    private List<Category> _categories = null!;

    // Selectors for ML badge elements
    private const string MLBadgeSelector = "[data-testid='ml-suggestion-badge']";
    private const string NoModelBadgeSelector = "[data-testid='no-model-badge']";
    private const string AnyBadgeSelector = $"{MLBadgeSelector}, {NoModelBadgeSelector}";

    [SetUp]
    public async Task SetUp()
    {
        _transactionsPage = new TransactionsPageModel(Page, BaseUrl);

        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create account with budget plan
        _testAccount = await SeedDataHelper.SeedAccountAsync(
            context,
            "ML Test Account",
            "NL91ABNA0417164300",
            1000m,
            "EUR");

        // Create categories for testing
        _categories = await SeedDataHelper.SeedCategoriesAsync(
            context,
            _testAccount.CurrentBudgetPlanId!.Value,
            "Food", "Transport", "Utilities", "Salary", "Shopping");

        // Seed 50 transactions: 10 per description pattern (5 patterns × 10 = 50)
        // Consistent patterns are critical so the ML model can learn reliable mappings
        var transactions = new List<Transaction>();
        var patterns = new[]
        {
            ("Grocery Store",   "Supermarkt"),
            ("Gas Station",     "Tankstation"),
            ("Electric Bill",   "Energiemaatschappij"),
            ("Salary Payment",  "Werkgever"),
            ("Online Shop",     "Webwinkel"),
        };

        var patternIndex = 0;
        foreach (var (description, counterparty) in patterns)
        {
            for (int i = 0; i < 10; i++)
            {
                var tx = new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = _testAccount.Id,
                    Amount = description == "Salary Payment" ? 2000m : -50m,
                    Date = DateTime.UtcNow.AddDays(-(i + patternIndex * 10)),
                    Description = description,
                    Counterparty = counterparty
                };
                transactions.Add(tx);
                context.Transactions.Add(tx);
            }
            patternIndex++;
        }
        await context.SaveChangesAsync();

        // Seed 10 labeled examples per category with consistent mapping
        var patternToCategory = new Dictionary<string, string>
        {
            ["Grocery Store"]  = "Food",
            ["Gas Station"]    = "Transport",
            ["Electric Bill"]  = "Utilities",
            ["Salary Payment"] = "Salary",
            ["Online Shop"]    = "Shopping",
        };

        foreach (var tx in transactions)
        {
            var categoryName = patternToCategory[tx.Description];
            var category = _categories.First(c => c.Name == categoryName);

            var labeledExample = new LabeledExample
            {
                Id = Guid.NewGuid(),
                TransactionId = tx.Id,
                CategoryId = category.Id,
                WasAutoApplied = false,
                AcceptedSuggestion = true,
                SuggestionConfidence = 0.90f
            };
            context.LabeledExamples.Add(labeledExample);
        }
        await context.SaveChangesAsync();

        // Train an actual ML model so suggestion badges appear in the UI
        var mlService = scope.ServiceProvider.GetRequiredService<IMLService>();
        await mlService.TrainModelAsync(90);

        // Flush WAL so the trained model is visible to subsequent DB connections (web server)
        await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)");
    }

    /// <summary>
    /// Waits for at least one ML suggestion badge (or no-model badge) to appear on the page.
    /// Returns the first ml-suggestion-badge element, or null if only no-model badges are shown.
    /// </summary>
    private async Task<Microsoft.Playwright.IElementHandle?> WaitForFirstSuggestionBadgeAsync()
    {
        // Wait for any badge to load (either suggestion or no-model)
        await Page.WaitForSelectorAsync(AnyBadgeSelector, new() { Timeout = 10_000 });
        return await Page.QuerySelectorAsync(MLBadgeSelector);
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLSuggestionBadge_DisplayedOnUnassignedTransactions()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        // Act — wait for any badge variant to appear
        await Page.WaitForSelectorAsync(AnyBadgeSelector, new() { Timeout = 10_000 });

        // Assert — at least one ML suggestion badge must exist (model was trained in SetUp)
        var mlBadge = await Page.QuerySelectorAsync(MLBadgeSelector);
        Assert.That(mlBadge, Is.Not.Null, "ML suggestion badge should be displayed for unassigned transactions when a model is available");
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLSuggestionBadge_ShowsFeatureImportanceTooltip()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        var badge = await WaitForFirstSuggestionBadgeAsync();
        Assert.That(badge, Is.Not.Null, "ML suggestion badge must be present (model was trained in SetUp)");

        // Act — hover over badge to trigger CSS tooltip
        await badge!.HoverAsync();

        // Assert — tooltip element is visible with "Based on" feature explanation text
        var tooltip = await Page.WaitForSelectorAsync(
            "[data-testid='suggestion-tooltip']",
            new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 3_000 });

        Assert.That(tooltip, Is.Not.Null, "Tooltip should appear on hover");
        var tooltipText = await tooltip!.InnerTextAsync();
        Assert.That(tooltipText, Does.Contain("Based on"), "Tooltip should show feature importance explanation");
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    [Category("Security")]
    public async Task MLSuggestionBadge_DoesNotUseHtmlTitleTooltipAttributes()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        var badge = await WaitForFirstSuggestionBadgeAsync();
        Assert.That(badge, Is.Not.Null, "ML suggestion badge must be present (model was trained in SetUp)");

        // Assert — badge must not expose tooltip content via attributes that could allow XSS
        var titleAttribute    = await badge!.GetAttributeAsync("title");
        var bootstrapToggle   = await badge.GetAttributeAsync("data-bs-toggle");
        var bootstrapHtml     = await badge.GetAttributeAsync("data-bs-html");

        Assert.That(titleAttribute,  Is.Null.Or.Empty,
            "Badge should not expose tooltip HTML content via title attribute");
        Assert.That(bootstrapToggle, Is.Null.Or.Empty,
            "Badge should not use Bootstrap tooltip initialization attributes for untrusted content");
        Assert.That(bootstrapHtml,   Is.Null.Or.Empty,
            "Badge should never enable HTML tooltip rendering for suggestion explanation content");
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLSuggestionBadge_AcceptButton_RecordsFeedbackAndHidesBadge()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        var badge = await WaitForFirstSuggestionBadgeAsync();
        Assert.That(badge, Is.Not.Null, "ML suggestion badge must be present (model was trained in SetUp)");

        var acceptButton = await badge!.QuerySelectorAsync("[data-testid='accept-suggestion']");
        Assert.That(acceptButton, Is.Not.Null, "Accept button should be present in badge");

        // Count labeled examples before accepting
        using var scopeBefore = Factory!.Services.CreateScope();
        var ctxBefore = scopeBefore.ServiceProvider.GetRequiredService<AppDbContext>();
        var countBefore = await ctxBefore.LabeledExamples.CountAsync();

        // Act — click Accept, then wait for the feedback API call to complete
        await acceptButton!.ClickAsync();
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        // Assert — a new LabeledExample feedback record was created
        using var scopeAfter = Factory!.Services.CreateScope();
        var ctxAfter = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
        var countAfter = await ctxAfter.LabeledExamples.CountAsync();
        Assert.That(countAfter, Is.GreaterThan(countBefore), "Accepting a suggestion should record a LabeledExample feedback entry");

        // Assert — the accepted badge should be hidden (MLSuggestionBadge hides itself on successful accept)
        var isAcceptedBadgeHidden = await badge!.IsHiddenAsync();
        Assert.That(isAcceptedBadgeHidden, Is.True, "Badge should be hidden after accepting suggestion");
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLSuggestionBadge_RejectButton_HidesBadgeLocally()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        var badge = await WaitForFirstSuggestionBadgeAsync();
        Assert.That(badge, Is.Not.Null, "ML suggestion badge must be present (model was trained in SetUp)");

        var rejectButton = await badge!.QuerySelectorAsync("[data-testid='reject-suggestion']");
        Assert.That(rejectButton, Is.Not.Null, "Reject button should be present in badge");

        // Count labeled examples before rejecting — reject does NOT call the feedback API
        using var scopeBefore = Factory!.Services.CreateScope();
        var ctxBefore = scopeBefore.ServiceProvider.GetRequiredService<AppDbContext>();
        var countBefore = await ctxBefore.LabeledExamples.CountAsync();

        // Act — click Reject (client-side dismiss only, no HTTP call)
        await rejectButton!.ClickAsync();
        // Allow Blazor to re-render; no network round-trip is expected
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        // Assert — no server-side feedback record created (reject is client-side dismiss only)
        using var scopeAfter = Factory!.Services.CreateScope();
        var ctxAfter = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
        var countAfter = await ctxAfter.LabeledExamples.CountAsync();
        Assert.That(countAfter, Is.EqualTo(countBefore), "Rejecting a suggestion should not create a new LabeledExample (client-side dismiss only)");
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task TransactionList_FilterBySuggestion_ShowsOnlyTransactionsWithSuggestions()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        // Wait for ML badges to load before applying the suggestion filter
        await Page.WaitForSelectorAsync(AnyBadgeSelector, new() { Timeout = 10_000 });

        // Apply "has suggestion" filter
        await Page.SelectOptionAsync("select[id='suggestion-filter']", "has-suggestion");
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        var rows = await _transactionsPage.GetTransactionRowsAsync();
        Assert.That(rows.Count, Is.GreaterThan(0), "Filtering to 'has-suggestion' should show unassigned rows with badges");
        foreach (var row in rows)
        {
            var rowBadge = await row.QuerySelectorAsync(MLBadgeSelector);
            Assert.That(rowBadge, Is.Not.Null, "All rows shown after 'has-suggestion' filter should have an ML badge");
        }
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task TransactionList_SortByConfidence_OrdersCorrectly()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        // Wait for all ML badges to load their confidence values
        await Page.WaitForSelectorAsync(AnyBadgeSelector, new() { Timeout = 10_000 });
        // Allow all async badge API calls to complete
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        // Apply confidence sort — Blazor re-orders rows based on stored confidence values
        await Page.SelectOptionAsync("select[id='sort-by']", "confidence-desc");
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

        var rows = await _transactionsPage.GetTransactionRowsAsync();
        var confidenceValues = new List<double>();
        foreach (var row in rows)
        {
            var rowBadge = await row.QuerySelectorAsync(MLBadgeSelector);
            if (rowBadge != null)
            {
                var confidenceText = await rowBadge.GetAttributeAsync("data-confidence");
                if (double.TryParse(confidenceText, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var confidence))
                    confidenceValues.Add(confidence);
            }
        }

        Assert.That(confidenceValues.Count, Is.GreaterThan(0), "Should have at least one badge with a confidence value");
        for (int i = 1; i < confidenceValues.Count; i++)
            Assert.That(confidenceValues[i], Is.LessThanOrEqualTo(confidenceValues[i - 1]),
                $"Row {i} confidence ({confidenceValues[i]}) should be <= row {i - 1} ({confidenceValues[i - 1]}) for descending order");
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLSuggestionBadge_ColorCoding_MatchesConfidenceThreshold()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        // Wait for at least one badge, then collect all
        await Page.WaitForSelectorAsync(AnyBadgeSelector, new() { Timeout = 10_000 });
        var badges = await Page.QuerySelectorAllAsync(MLBadgeSelector);
        Assert.That(badges.Count, Is.GreaterThan(0), "At least one ML suggestion badge should be visible");

        // Assert — verify each badge's CSS class matches its confidence level
        foreach (var b in badges)
        {
            var confidenceAttr = await b.GetAttributeAsync("data-confidence");
            if (string.IsNullOrEmpty(confidenceAttr)) continue;

            var confidence = double.Parse(confidenceAttr, System.Globalization.CultureInfo.InvariantCulture);
            var className = await b.GetAttributeAsync("class") ?? "";

            if (confidence >= 0.80)
                Assert.That(className, Does.Contain("success"),
                    $"High confidence ({confidence:P0}) should use 'bg-success' (green)");
            else if (confidence >= 0.60)
                Assert.That(className, Does.Contain("warning"),
                    $"Medium confidence ({confidence:P0}) should use 'bg-warning' (yellow)");
            else
                Assert.That(className, Does.Contain("secondary"),
                    $"Low confidence ({confidence:P0}) should use 'bg-secondary' (gray)");
        }
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLModelInfo_DisplaysActiveModelDetails()
    {
        // Act — navigate to ML model info admin page
        await Page.GotoAsync($"{BaseUrl}/admin/ml");
        await Page.WaitForSelectorAsync("[data-testid='model-info']", new() { Timeout = 5_000 });

        // Assert — model metadata is displayed (a model was trained in SetUp)
        var modelVersionElement  = await Page.WaitForSelectorAsync("[data-testid='model-version']",  new() { Timeout = 5_000 });
        var accuracyElement      = await Page.WaitForSelectorAsync("[data-testid='model-accuracy']", new() { Timeout = 5_000 });
        var lastTrainedElement   = await Page.WaitForSelectorAsync("[data-testid='last-trained']",   new() { Timeout = 5_000 });

        // Retrieve text before Assert.Multiple (async lambdas are not supported)
        var modelVersionText = await modelVersionElement!.InnerTextAsync();
        var accuracyText     = await accuracyElement!.InnerTextAsync();
        var lastTrainedText  = await lastTrainedElement!.InnerTextAsync();

        Assert.Multiple(() =>
        {
            Assert.That(modelVersionText, Is.Not.Empty,      "Model version should be displayed");
            Assert.That(accuracyText,     Does.Contain("%"), "Accuracy should be displayed as percentage");
            Assert.That(lastTrainedText,  Is.Not.Empty,      "Last trained date should be displayed");
        });
    }
}
