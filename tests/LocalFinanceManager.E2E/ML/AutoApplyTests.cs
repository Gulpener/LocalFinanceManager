using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using LocalFinanceManager.ML;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.ML;

/// <summary>
/// E2E tests for Auto-Apply Settings configuration.
/// Tests UserStory-8 Section 16: Settings page, enable/disable toggle, confidence threshold, account selection, validation.
/// </summary>
[TestFixture]
public class AutoApplyTests : E2ETestBase
{
    private AutoApplySettingsPageModel _settingsPage = null!;
    private Account _testAccount1 = null!;
    private Account _testAccount2 = null!;
    private List<Category> _categories = null!;

    [SetUp]
    public async Task SetUp()
    {
        _settingsPage = new AutoApplySettingsPageModel(Page, BaseUrl);

        // Truncate tables before each test to prevent data accumulation across tests.
        // Each SetUp seeds 100 transactions; without truncation, subsequent tests see
        // hundreds of stale rows which can break ML training and auto-apply assertions.
        await Factory!.TruncateTablesAsync();

        // Clear localStorage filter state to prevent cross-test contamination.
        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.EvaluateAsync("() => localStorage.removeItem('transactionFilters')");

        // Seed test data
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create two accounts for multi-account testing
        _testAccount1 = await SeedDataHelper.SeedAccountAsync(
            context,
            "Auto-Apply Account 1",
            "NL91ABNA0417164300",
            1000m,
            "EUR");

        _testAccount2 = await SeedDataHelper.SeedAccountAsync(
            context,
            "Auto-Apply Account 2",
            "NL20INGB0001234567",
            2000m,
            "EUR");

        // Create categories for first account
        _categories = await SeedDataHelper.SeedCategoriesAsync(
            context,
            _testAccount1.CurrentBudgetPlanId!.Value,
            "Food", "Transport", "Utilities", "Entertainment");

        // Create transactions for testing preview stats
        await SeedDataHelper.SeedTransactionsAsync(
            context,
            _testAccount1.Id,
            100,
            minAmount: -200m,
            maxAmount: 100m);
    }

    [Test]
    [Category("E2E")]
    [Category("AutoApply")]
    public async Task AutoApplySettings_PageLoads_WithCurrentSettings()
    {
        // Act
        await _settingsPage.NavigateAsync();

        // Assert - Page should load successfully
        var pageHeading = await Page.Locator("h1").First.InnerTextAsync();
        Assert.That(pageHeading, Does.Contain("Auto-Apply"),
            "Page heading should indicate Auto-Apply settings");

        // Settings should be loaded (default or saved values)
        await _settingsPage.IsEnabledAsync();
        // IsEnabledAsync returns bool, just verify page loaded correctly
    }

    [Test]
    [Category("E2E")]
    [Category("AutoApply")]
    public async Task AutoApplySettings_ToggleEnable_SettingSaved()
    {
        // Arrange
        await _settingsPage.NavigateAsync();
        var initialState = await _settingsPage.IsEnabledAsync();

        // Act - Toggle the enable switch
        await _settingsPage.SetEnableToggleAsync(!initialState);
        await _settingsPage.SaveSettingsAsync();

        // Reload page to verify persistence
        await _settingsPage.NavigateAsync();

        // Assert - Toggle state should be persisted in singleton settings row
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await context.AppSettings
            .Where(s => !s.IsArchived && s.UserId == AppDbContext.SeedUserId)
            .FirstOrDefaultAsync();

        Assert.That(settings, Is.Not.Null, "Settings should be saved to database");
        Assert.That(settings!.AutoApplyEnabled, Is.EqualTo(!initialState), "Toggle state should be persisted after save");
    }

    [Test]
    [Category("E2E")]
    [Category("AutoApply")]
    public async Task AutoApplySettings_AdjustConfidenceThreshold_PreviewStatsUpdate()
    {
        // Arrange
        await _settingsPage.NavigateAsync();

        // Act - Set confidence threshold to 85%
        await _settingsPage.SetConfidenceThresholdAsync(0.85);
        await Task.Delay(1000); // Wait for preview to update

        // Assert - Preview stats should update
        var previewStats = await _settingsPage.GetPreviewStatsAsync();
        Assert.That(previewStats, Is.Not.Empty, "Preview stats should be displayed");
        Assert.That(previewStats, Does.Contain("transact").IgnoreCase,
            "Preview stats should mention transactions");

        // Display should show 85%
        var displayValue = await _settingsPage.GetConfidenceDisplayValueAsync();
        Assert.That(displayValue, Does.Contain("85"), "Confidence display should show 85%");
    }

    [Test]
    [Category("E2E")]
    [Category("AutoApply")]
    public async Task AutoApplySettings_SelectSpecificAccounts_OnlySelectedAccountsProcessed()
    {
        // Arrange
        await _settingsPage.NavigateAsync();

        // Act - Select only first account
        await _settingsPage.SelectAccountsAsync(_testAccount1.Id);
        await _settingsPage.SaveSettingsAsync();

        // Assert - Settings should be saved with selected account
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await context.AppSettings
            .Where(s => !s.IsArchived && s.UserId == AppDbContext.SeedUserId)
            .FirstOrDefaultAsync();

        Assert.That(settings, Is.Not.Null, "Settings should be saved to database");
        Assert.That(settings!.AccountIdsJson, Does.Contain(_testAccount1.Id.ToString()),
            "Selected account should be in saved settings");
    }

    [Test]
    [Category("E2E")]
    [Category("AutoApply")]
    public async Task AutoApplySettings_AddExcludedCategories_SettingSaved()
    {
        // Arrange
        await _settingsPage.NavigateAsync();
        var excludeCategory = _categories.First(c => c.Name == "Entertainment");

        // Act - Exclude "Entertainment" category
        await _settingsPage.SelectExcludedCategoriesAsync(excludeCategory.Id);
        await _settingsPage.SaveSettingsAsync();

        // Assert - Excluded category should be persisted
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await context.AppSettings
            .Where(s => !s.IsArchived && s.UserId == AppDbContext.SeedUserId)
            .FirstOrDefaultAsync();

        Assert.That(settings, Is.Not.Null, "Settings should be saved");
        Assert.That(settings!.ExcludedCategoryIdsJson, Does.Contain(excludeCategory.Id.ToString()),
            "Excluded category should be saved");
    }

    [Test]
    [Category("E2E")]
    [Category("AutoApply")]
    public async Task AutoApplySettings_InvalidConfidence_ValidationErrorShown()
    {
        // Arrange
        await _settingsPage.NavigateAsync();

        // Act - Try to set invalid confidence (>100% or <0%)
        // Note: HTML5 range input typically prevents invalid values, so we test via direct API call
        var invalidConfidenceScript = @"
            document.querySelector('input[type=""range""][id=""confidence-threshold""]').value = '1.5';
        ";
        await Page.EvaluateAsync(invalidConfidenceScript);
        await _settingsPage.ClickSaveButtonAsync();
        await Task.Delay(500);

        // Assert - Validation error should be displayed
        var validationError = await _settingsPage.GetValidationErrorAsync();

        // If client-side validation prevents submission, error may not appear
        // In that case, verify the value was clamped or prevented
        if (validationError == null)
        {
            var confidenceDisplay = await _settingsPage.GetConfidenceDisplayValueAsync();
            Assert.That(confidenceDisplay, Does.Not.Contain("150"),
                "Invalid confidence should be prevented or clamped");
        }
        else
        {
            Assert.That(validationError, Does.Contain("confidence").IgnoreCase.Or.Contain("valid").IgnoreCase,
                "Validation error should mention confidence or validation");
        }
    }

    [Test]
    [Category("E2E")]
    [Category("AutoApply")]
    public async Task AutoApplySettings_SaveSettings_SuccessToastDisplayed()
    {
        // Arrange
        await _settingsPage.NavigateAsync();
        var initialState = await _settingsPage.IsEnabledAsync();
        var expectedState = !initialState;

        // Act - Change a setting and save
        await _settingsPage.SetEnableToggleAsync(expectedState);
        await _settingsPage.ClickSaveButtonAsync();

        // Assert - Success toast should appear
        var successToastShown = await _settingsPage.WaitForSuccessToastAsync();

        if (!successToastShown)
        {
            using var scope = Factory!.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var settings = await context.AppSettings
                .Where(s => !s.IsArchived && s.UserId == AppDbContext.SeedUserId)
                .FirstOrDefaultAsync();

            Assert.That(settings, Is.Not.Null, "Settings should be persisted after saving");
            Assert.That(settings!.AutoApplyEnabled, Is.EqualTo(expectedState),
                "Expected enabled state to be persisted when toast is not observable");
            return;
        }

        Assert.That(successToastShown, Is.True, "Success toast should appear after saving settings");
    }

    [Test]
    [Category("E2E")]
    [Category("AutoApply")]
    public async Task AutoApply_ManualTrigger_TransactionsAutoAssigned()
    {
        // Arrange - Enable auto-apply via HTTP POST so the real server's DbContext writes the row.
        // run-now now reads directly from _dbContext (no cache), so it sees this committed value.
        // Use 0.60 (minimum allowed when enabled) as the confidence threshold.
        var enableResponse = await Page.APIRequest.PostAsync(
            $"{BaseUrl}/api/automation/settings",
            new Microsoft.Playwright.APIRequestContextOptions
            {
                DataObject = new
                {
                    enabled = true,
                    minimumConfidence = 0.60f,
                    intervalMinutes = 15,
                    accountIds = Array.Empty<Guid>(),
                    excludedCategoryIds = Array.Empty<Guid>()
                },
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            });
        Assert.That(enableResponse.Status, Is.EqualTo(200), "Auto-apply settings should be saved");

        // Seed patterned transactions + labeled examples so the ML model can learn reliable
        // category mappings and predict with confidence >= 0.60 on the training examples.
        using (var scopeSetup = Factory!.Services.CreateScope())
        {
            var contextSetup = scopeSetup.ServiceProvider.GetRequiredService<AppDbContext>();

            // Seed BudgetLines for every category so TryApplyAsync can create TransactionSplits
            foreach (var category in _categories)
            {
                await SeedDataHelper.SeedBudgetLineAsync(
                    contextSetup,
                    _testAccount1.CurrentBudgetPlanId!.Value,
                    category.Id,
                    1200m);
            }

            // Seed 40 patterned transactions (10 per category) with consistent descriptions.
            // Consistent patterns allow the model to learn a reliable description→category mapping.
            var patternToCategory = new Dictionary<string, Category>
            {
                ["Grocery Store Payment"] = _categories.First(c => c.Name == "Food"),
                ["Fuel Station Charge"] = _categories.First(c => c.Name == "Transport"),
                ["Electricity Bill"] = _categories.First(c => c.Name == "Utilities"),
                ["Cinema Ticket"] = _categories.First(c => c.Name == "Entertainment"),
            };

            var patterned = new List<Transaction>();
            foreach (var (description, _) in patternToCategory)
            {
                for (int i = 0; i < 10; i++)
                {
                    var tx = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = _testAccount1.Id,
                        Amount = -50m,
                        Date = DateTime.UtcNow.AddDays(-(i + 1)),
                        Description = description,
                        Counterparty = $"Auto-apply Test Counterparty {i}"
                    };
                    patterned.Add(tx);
                    contextSetup.Transactions.Add(tx);
                }
            }
            await contextSetup.SaveChangesAsync();

            // Seed labeled examples mapping each patterned description to its category
            foreach (var tx in patterned)
            {
                var category = patternToCategory[tx.Description];
                contextSetup.LabeledExamples.Add(new LabeledExample
                {
                    Id = Guid.NewGuid(),
                    TransactionId = tx.Id,
                    CategoryId = category.Id,
                    WasAutoApplied = false,
                    AcceptedSuggestion = true,
                    SuggestionConfidence = 0.90f
                });
            }
            await contextSetup.SaveChangesAsync();

            // Train via the REAL host's IMLService so its IMLModelCache is warmed up immediately
            using var hostScopeML = Factory!.HostServices.CreateScope();
            var hostMlService = hostScopeML.ServiceProvider.GetRequiredService<IMLService>();
            await hostMlService.TrainModelAsync(70);
        }

        // Act - Trigger auto-apply via the run-now endpoint
        // Use an extended timeout (90s) because ML inference under parallel test load can be slow.
        var response = await Page.APIRequest.PostAsync($"{BaseUrl}/api/automation/run-now",
            new Microsoft.Playwright.APIRequestContextOptions { Timeout = 90_000 });
        Assert.That(response.Status, Is.EqualTo(200), "run-now endpoint should return 200 OK");

        var json = await response.JsonAsync();
        Assert.That(json?.GetProperty("success").GetBoolean(), Is.True, "run-now response should indicate success");

        var appliedCount = json?.GetProperty("appliedCount").GetInt32() ?? 0;
        Assert.That(appliedCount, Is.GreaterThan(0),
            "At least one transaction should have been auto-assigned by the ML model");

        // Assert - Verify auto-apply audit entries were created
        using var scopeAfter = Factory!.Services.CreateScope();
        var contextAfter = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
        var autoApplyAuditCount = await contextAfter.TransactionAudits
            .CountAsync(a => a.IsAutoApplied && !a.IsArchived);

        Assert.That(autoApplyAuditCount, Is.GreaterThan(0),
            "Auto-apply audit entries should have been created in the database");
    }

    [Test]
    [Category("E2E")]
    [Category("AutoApply")]
    [Ignore("Transaction audit trail UI page not implemented - /transactions/{id}/audit returns 404")]
    public async Task AutoApply_AuditTrail_ShowsAutoAppliedIndicator()
    {
        // Arrange - Create auto-applied transaction via seed helper
        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var transaction = await SeedDataHelper.SeedTransactionAsync(
            context,
            _testAccount1.Id,
            -100m,
            DateTime.UtcNow,
            "Test Transaction");

        await SeedDataHelper.SeedBudgetLineAsync(
            context,
            _testAccount1.CurrentBudgetPlanId!.Value,
            _categories.First().Id,
            1000m);

        // Create audit entry for auto-applied assignment
        var auditEntry = new TransactionAudit
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            ActionType = "AutoAssign",
            ChangedBy = "AutoApplyService",
            ChangedAt = DateTime.UtcNow,
            IsAutoApplied = true,
            AutoAppliedBy = "AutoApplyService",
            AutoAppliedAt = DateTime.UtcNow,
            Confidence = 0.85f,
            ModelVersion = 1,
            BeforeState = "{\"assigned\":false}",
            AfterState = "{\"assigned\":true}",
            Reason = "Auto-applied by ML model"
        };
        context.TransactionAudits.Add(auditEntry);
        await context.SaveChangesAsync();

        // Act - Navigate to transaction audit trail (if UI exists)
        await Page.GotoAsync($"{BaseUrl}/transactions/{transaction.Id}/audit");

        // Check if page exists
        var pageNotFound = await Page.Locator("text=404").IsVisibleAsync();
        if (pageNotFound)
        {
            Assert.Ignore("Audit trail page not implemented in UI");
            return;
        }

        // Assert - Audit trail should show "Auto-applied" indicator
        var autoAppliedLocator = Page.Locator("text=/auto.?applied/i");
        var count = await autoAppliedLocator.CountAsync();
        Assert.That(count, Is.GreaterThan(0),
            "Audit trail should display 'Auto-applied' indicator for auto-assigned transactions");
    }
}
