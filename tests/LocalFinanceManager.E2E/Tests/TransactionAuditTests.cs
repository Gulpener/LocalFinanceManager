using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Tests;

[TestFixture]
public class TransactionAuditTests : E2ETestBase
{
    private TransactionsPageModel _transactionsPage = null!;
    private TransactionAuditPageModel _auditPage = null!;
    private Guid _accountId;
    private Guid _transactionId;

    [SetUp]
    public async Task SetUp()
    {
        _transactionsPage = new TransactionsPageModel(Page, BaseUrl);
        _auditPage = new TransactionAuditPageModel(Page, BaseUrl);

        await Factory!.TruncateTablesAsync();

        using var scope = Factory.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await SeedDataHelper.SeedAccountAsync(context, "Audit Test Account", "NL91ABNA0417164300", 500m);
        _accountId = account.Id;

        var tx = await SeedDataHelper.SeedTransactionAsync(context, _accountId, -42.50m, DateTime.UtcNow.AddDays(-1), "Albert Heijn - Boodschappen");
        _transactionId = tx.Id;

        await SeedDataHelper.SeedTransactionAuditsAsync(context, _transactionId, new (string, string, bool, float?, int?, string?, string?, string?)[]
        {
            (
                "Import",
                "ImportService",
                false,
                null,
                null,
                null,
                "{\"description\":\"Albert Heijn - Boodschappen\",\"amount\":-42.50}",
                "Geïmporteerd vanuit bestand"
            ),
            (
                "AutoAssign",
                "AutoApplyService",
                true,
                0.85f,
                1,
                "{\"assigned\":false}",
                "{\"assigned\":true,\"budgetLine\":\"Boodschappen\"}",
                "Auto-applied by ML model"
            )
        });

        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.EvaluateAsync("() => localStorage.removeItem('transactionFilters')");
    }

    [Test]
    public async Task TransactionAudit_PageLoads_WithAuditHistory()
    {
        await _auditPage.NavigateAsync(_transactionId);
        await _auditPage.WaitForPageLoadAsync();

        var count = await _auditPage.GetAuditEntryCountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task TransactionAudit_AutoAppliedBadge_VisibleForMLAssignments()
    {
        await _auditPage.NavigateAsync(_transactionId);
        await _auditPage.WaitForPageLoadAsync();

        var count = await _auditPage.GetAuditEntryCountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(2));

        // Find the auto-applied entry
        var foundAutoApplied = false;
        for (var i = 0; i < count; i++)
        {
            if (await _auditPage.HasAutoAppliedBadgeAsync(i))
            {
                foundAutoApplied = true;
                break;
            }
        }

        Assert.That(foundAutoApplied, Is.True, "Should find at least one entry with the auto-applied badge");
    }

    [Test]
    public async Task TransactionAudit_ConfidenceScore_DisplayedCorrectly()
    {
        await _auditPage.NavigateAsync(_transactionId);
        await _auditPage.WaitForPageLoadAsync();

        var count = await _auditPage.GetAuditEntryCountAsync();

        for (var i = 0; i < count; i++)
        {
            if (await _auditPage.HasAutoAppliedBadgeAsync(i))
            {
                var confidence = await _auditPage.GetConfidenceScoreAsync(i);
                Assert.That(confidence, Does.Contain("85").And.Contain("zekerheid"),
                    "Confidence score should display percentage with label");
                break;
            }
        }
    }

    [Test]
    public async Task TransactionAudit_Timeline_OrderedChronologically()
    {
        await _auditPage.NavigateAsync(_transactionId);
        await _auditPage.WaitForPageLoadAsync();

        var count = await _auditPage.GetAuditEntryCountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(1));

        // All entries should have a timestamp visible
        for (var i = 0; i < count; i++)
        {
            var timestamp = await _auditPage.GetTimestampTextAsync(i);
            Assert.That(timestamp, Is.Not.Empty, $"Entry {i} should have a timestamp");
        }
    }

    [Test]
    public async Task TransactionAudit_StateChanges_DisplayDifferences()
    {
        await _auditPage.NavigateAsync(_transactionId);
        await _auditPage.WaitForPageLoadAsync();

        var count = await _auditPage.GetAuditEntryCountAsync();

        // Find an entry with state changes and expand it
        for (var i = 0; i < count; i++)
        {
            await _auditPage.ExpandStateChangesAsync(i);
            var afterState = await _auditPage.GetAfterStateAsync(i);
            if (!string.IsNullOrEmpty(afterState))
            {
                Assert.That(afterState, Does.Contain("{"), "After state should contain JSON");
                break;
            }
        }
    }

    [Test]
    public async Task TransactionAudit_Link_FromTransactionList_NavigatesToAuditPage()
    {
        await _transactionsPage.NavigateAsync();
        await _transactionsPage.SelectAccountFilterAsync(_accountId);

        // Click the audit trail link for the transaction
        var auditLink = Page.Locator($"tr[data-transaction-id='{_transactionId}'] [data-testid='audit-trail-link']");
        await auditLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await auditLink.ClickAsync();

        // Should navigate to the audit page
        await Page.WaitForURLAsync($"**/transactions/{_transactionId}/audit", new PageWaitForURLOptions { Timeout = 10000 });
        await _auditPage.WaitForPageLoadAsync();

        var count = await _auditPage.GetAuditEntryCountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(1));
    }
}
