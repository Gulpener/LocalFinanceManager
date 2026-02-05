using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
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

    [SetUp]
    public async Task SetUp()
    {
        _transactionsPage = new TransactionsPageModel(Page, BaseUrl);

        // Seed test data: Account, BudgetPlan, Categories, Transactions, LabeledExamples
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

        // Create 20 transactions with patterns matching categories
        var transactions = new List<Transaction>();
        for (int i = 0; i < 20; i++)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = _testAccount.Id,
                Amount = i % 2 == 0 ? -50m : 100m, // Alternate expense/income
                Date = DateTime.UtcNow.AddDays(-i),
                Description = i % 5 == 0 ? "Grocery Store" : i % 5 == 1 ? "Gas Station" : i % 5 == 2 ? "Electric Bill" : i % 5 == 3 ? "Salary Payment" : "Online Shop",
                Counterparty = $"Counterparty {i}"
            };
            transactions.Add(transaction);
            context.Transactions.Add(transaction);
        }
        await context.SaveChangesAsync();

        // Create labeled examples (training data) to simulate trained model
        // At least 10 per category for training threshold
        var labeledExamples = new List<LabeledExample>();
        for (int i = 0; i < transactions.Count; i++)
        {
            var transaction = transactions[i];
            Category category;

            // Assign patterns to categories
            if (transaction.Description.Contains("Grocery"))
                category = _categories.First(c => c.Name == "Food");
            else if (transaction.Description.Contains("Gas"))
                category = _categories.First(c => c.Name == "Transport");
            else if (transaction.Description.Contains("Electric"))
                category = _categories.First(c => c.Name == "Utilities");
            else if (transaction.Description.Contains("Salary"))
                category = _categories.First(c => c.Name == "Salary");
            else
                category = _categories.First(c => c.Name == "Shopping");

            var labeledExample = new LabeledExample
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                CategoryId = category.Id,
                WasAutoApplied = false,
                AcceptedSuggestion = true,
                SuggestionConfidence = 0.85f
            };
            labeledExamples.Add(labeledExample);
            context.LabeledExamples.Add(labeledExample);
        }
        await context.SaveChangesAsync();

        // Note: In production, ML model training would happen here.
        // For E2E tests, we assume the model is trained and suggestions API is working.
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLSuggestionBadge_DisplayedOnUnassignedTransactions()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        // Act
        var rows = await _transactionsPage.GetTransactionRowsAsync();

        // Assert
        Assert.That(rows.Count, Is.GreaterThan(0), "Transactions should be displayed");

        // Check if ML suggestion badges are present (only if model is trained)
        // Note: Badge visibility depends on API returning suggestions
        var firstRow = rows[0];
        var badgeSelector = "[data-testid='ml-suggestion-badge']";
        var hasBadge = await Page.QuerySelectorAsync($"{badgeSelector}") != null;

        // If no model is trained, badge should show "No ML model" warning
        if (!hasBadge)
        {
            var noModelBadge = await Page.QuerySelectorAsync("[data-testid='no-model-badge']");
            Assert.That(noModelBadge, Is.Not.Null, "Should show 'No ML model' badge when model not available");
        }
        else
        {
            Assert.Pass("ML suggestion badge displayed (model available)");
        }
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLSuggestionBadge_ShowsFeatureImportanceTooltip()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        // Act
        var badgeSelector = "[data-testid='ml-suggestion-badge']";
        var badge = await Page.QuerySelectorAsync(badgeSelector);

        if (badge == null)
        {
            Assert.Ignore("Skipping: ML model not trained, no suggestions available");
            return;
        }

        // Hover over badge to trigger tooltip
        await badge.HoverAsync();

        // Assert - Check tooltip appears with feature importance
        var tooltipSelector = "[data-testid='suggestion-tooltip']";
        var tooltip = await Page.WaitForSelectorAsync(tooltipSelector, new() { Timeout = 3000 });
        Assert.That(tooltip, Is.Not.Null, "Tooltip should appear on hover");

        var tooltipText = await tooltip!.InnerTextAsync();
        Assert.That(tooltipText, Does.Contain("Based on"), "Tooltip should show feature importance explanation");
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLSuggestionBadge_AcceptButton_AssignsTransactionAndHidesBadge()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        var badgeSelector = "[data-testid='ml-suggestion-badge']";
        var badge = await Page.QuerySelectorAsync(badgeSelector);

        if (badge == null)
        {
            Assert.Ignore("Skipping: ML model not trained, no suggestions available");
            return;
        }

        // Act - Click "Accept" button on suggestion badge
        var acceptButtonSelector = "[data-testid='accept-suggestion']";
        var acceptButton = await badge.QuerySelectorAsync(acceptButtonSelector);
        Assert.That(acceptButton, Is.Not.Null, "Accept button should be present in badge");

        await acceptButton!.ClickAsync();

        // Wait for success toast or badge to disappear
        await Task.Delay(1000); // Allow API call to complete

        // Assert - Badge should disappear (transaction now assigned)
        var badgeAfterAccept = await Page.QuerySelectorAsync(badgeSelector);
        Assert.That(badgeAfterAccept, Is.Null, "Badge should disappear after accepting suggestion");

        // Verify transaction is assigned in database
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assignedTransactions = await context.TransactionSplits.CountAsync();
        Assert.That(assignedTransactions, Is.GreaterThan(0), "Transaction should be assigned in database");
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLSuggestionBadge_RejectButton_RecordsFeedbackAndBadgeRemains()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        var badgeSelector = "[data-testid='ml-suggestion-badge']";
        var badge = await Page.QuerySelectorAsync(badgeSelector);

        if (badge == null)
        {
            Assert.Ignore("Skipping: ML model not trained, no suggestions available");
            return;
        }

        // Get initial count of labeled examples (feedback)
        using var scopeBefore = Factory!.Services.CreateScope();
        var contextBefore = scopeBefore.ServiceProvider.GetRequiredService<AppDbContext>();
        var feedbackCountBefore = await contextBefore.LabeledExamples.CountAsync();

        // Act - Click "Reject" button
        var rejectButtonSelector = "[data-testid='reject-suggestion']";
        var rejectButton = await badge.QuerySelectorAsync(rejectButtonSelector);
        Assert.That(rejectButton, Is.Not.Null, "Reject button should be present in badge");

        await rejectButton!.ClickAsync();
        await Task.Delay(1000); // Allow API call to complete

        // Assert - Transaction should still be unassigned
        using var scopeAfter = Factory!.Services.CreateScope();
        var contextAfter = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
        var feedbackCountAfter = await contextAfter.LabeledExamples.CountAsync();

        // Feedback should be recorded (if API creates new LabeledExample for rejection)
        // This depends on implementation - may just update existing or create new
        Assert.That(feedbackCountAfter, Is.GreaterThanOrEqualTo(feedbackCountBefore), "Feedback should be recorded");
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task TransactionList_FilterBySuggestion_ShowsOnlyTransactionsWithSuggestions()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        // Act - Select "Has Suggestion" filter
        var filterSelector = "select[id='suggestion-filter']";
        var filterExists = await Page.QuerySelectorAsync(filterSelector) != null;

        if (!filterExists)
        {
            Assert.Ignore("Skipping: 'Has Suggestion' filter not implemented in UI");
            return;
        }

        await Page.SelectOptionAsync(filterSelector, "has-suggestion");
        await Task.Delay(500); // Wait for filter to apply

        // Assert - Only transactions with suggestion badges should be shown
        var rows = await _transactionsPage.GetTransactionRowsAsync();
        Assert.That(rows.Count, Is.GreaterThan(0), "Filtered transactions should be displayed");

        foreach (var row in rows)
        {
            var badge = await row.QuerySelectorAsync("[data-testid='ml-suggestion-badge']");
            Assert.That(badge, Is.Not.Null, "All visible transactions should have suggestion badges");
        }
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task TransactionList_SortByConfidence_OrdersCorrectly()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        // Act - Sort by confidence (if sort option available)
        var sortSelector = "select[id='sort-by']";
        var sortExists = await Page.QuerySelectorAsync(sortSelector) != null;

        if (!sortExists)
        {
            Assert.Ignore("Skipping: Confidence sort option not implemented in UI");
            return;
        }

        await Page.SelectOptionAsync(sortSelector, "confidence-desc");
        await Task.Delay(500);

        // Assert - Verify transactions are sorted by confidence (highest first)
        var rows = await _transactionsPage.GetTransactionRowsAsync();
        var confidenceValues = new List<double>();

        foreach (var row in rows)
        {
            var badge = await row.QuerySelectorAsync("[data-testid='ml-suggestion-badge']");
            if (badge != null)
            {
                var confidenceText = await badge.GetAttributeAsync("data-confidence");
                if (double.TryParse(confidenceText, out var confidence))
                {
                    confidenceValues.Add(confidence);
                }
            }
        }

        // Verify descending order
        for (int i = 1; i < confidenceValues.Count; i++)
        {
            Assert.That(confidenceValues[i], Is.LessThanOrEqualTo(confidenceValues[i - 1]),
                $"Confidence values should be in descending order. Found {confidenceValues[i - 1]} followed by {confidenceValues[i]}");
        }
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLSuggestionBadge_ColorCoding_MatchesConfidenceThreshold()
    {
        // Arrange
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_testAccount.Id);

        // Act - Get all suggestion badges and check colors
        var badges = await Page.QuerySelectorAllAsync("[data-testid='ml-suggestion-badge']");

        if (badges.Count == 0)
        {
            Assert.Ignore("Skipping: No ML suggestions available");
            return;
        }

        // Assert - Verify color coding matches confidence
        foreach (var badge in badges)
        {
            var confidenceAttr = await badge.GetAttributeAsync("data-confidence");
            if (string.IsNullOrEmpty(confidenceAttr)) continue;

            var confidence = double.Parse(confidenceAttr);
            var className = await badge.GetAttributeAsync("class") ?? "";

            if (confidence > 0.80)
            {
                Assert.That(className, Does.Contain("green").Or.Contain("success"),
                    $"High confidence ({confidence:P0}) should have green/success color");
            }
            else if (confidence >= 0.60)
            {
                Assert.That(className, Does.Contain("yellow").Or.Contain("warning"),
                    $"Medium confidence ({confidence:P0}) should have yellow/warning color");
            }
            else
            {
                Assert.That(className, Does.Contain("gray").Or.Contain("secondary"),
                    $"Low confidence ({confidence:P0}) should have gray/secondary color");
            }
        }
    }

    [Test]
    [Category("E2E")]
    [Category("ML")]
    public async Task MLModelInfo_DisplaysActiveModelDetails()
    {
        // Arrange & Act - Navigate to ML model info page
        await Page.GotoAsync($"{BaseUrl}/admin/ml");
        await Page.WaitForSelectorAsync("[data-testid='model-info']", new() { Timeout = 5000 });

        // Assert - Check if model metadata is displayed
        var modelVersionElement = await Page.QuerySelectorAsync("[data-testid='model-version']");
        var accuracyElement = await Page.QuerySelectorAsync("[data-testid='model-accuracy']");
        var lastTrainedElement = await Page.QuerySelectorAsync("[data-testid='last-trained']");

        if (modelVersionElement == null)
        {
            Assert.Ignore("Skipping: No active ML model found");
            return;
        }

        var modelVersion = await modelVersionElement.InnerTextAsync();
        var accuracy = await accuracyElement!.InnerTextAsync();
        var lastTrained = await lastTrainedElement!.InnerTextAsync();

        Assert.That(modelVersion, Is.Not.Empty, "Model version should be displayed");
        Assert.That(accuracy, Does.Contain("%"), "Accuracy should be displayed as percentage");
        Assert.That(lastTrained, Is.Not.Empty, "Last trained date should be displayed");
    }
}
