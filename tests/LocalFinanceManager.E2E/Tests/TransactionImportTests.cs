using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.E2E.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using System.Globalization;

namespace LocalFinanceManager.E2E.Tests;

[TestFixture]
public class TransactionImportTests : E2ETestBase
{
    private ImportModalPageModel _importPage = null!;
    private Guid _accountId;

    private static string ToIsoDate(DateTime date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    [SetUp]
    public async Task SetUp()
    {
        _importPage = new ImportModalPageModel(Page, BaseUrl);
        await Factory!.TruncateTablesAsync();

        using var scope = Factory.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await SeedDataHelper.SeedAccountAsync(context, "Import Test Account", "NL91ABNA0417164300", 1000m);
        _accountId = account.Id;
    }

    [Test]
    public async Task UploadCsv_ShowsPreview_WithExpectedCount()
    {
        var firstDate = DateTime.UtcNow.Date.AddDays(-3);
        var secondDate = firstDate.AddDays(1);
        var thirdDate = firstDate.AddDays(2);
        var csv = $"Date,Amount,Description,Counterparty,ExternalId\n{ToIsoDate(firstDate)},-12.50,Coffee,Store,EXT-1\n{ToIsoDate(secondDate)},-20.00,Lunch,Cafe,EXT-2\n{ToIsoDate(thirdDate)},2500.00,Salary,Employer,EXT-3";

        await _importPage.NavigateAsync();
        await _importPage.SelectAccountAsync(_accountId);
        await _importPage.SelectFileFormatAsync("csv");
        await _importPage.UploadFileContentAsync("import.csv", "text/csv", csv);
        await _importPage.ClickPreviewAsync();

        Assert.That(await _importPage.IsPreviewVisibleAsync(), Is.True);
        Assert.That(await _importPage.GetPreviewCountAsync(), Is.EqualTo(3));

        await _importPage.TakeScreenshotAsync("import-preview");
    }

    [Test]
    public async Task Csv_AutoMapping_DetectsStandardHeaders()
    {
        var csvDate = DateTime.UtcNow.Date.AddDays(-2);
        var csv = $"Date,Amount,Description,Counterparty,ExternalId\n{ToIsoDate(csvDate)},-12.50,Coffee,Store,EXT-1";

        await _importPage.NavigateAsync();
        await _importPage.SelectAccountAsync(_accountId);
        await _importPage.UploadFileContentAsync("mapping.csv", "text/csv", csv);
        await _importPage.ClickPreviewAsync();

        var dateValue = await Page.InputValueAsync("xpath=//label[contains(normalize-space(),'Datum kolom')]/following-sibling::select[1]");
        var amountValue = await Page.InputValueAsync("xpath=//label[contains(normalize-space(),'Bedrag kolom')]/following-sibling::select[1]");
        Assert.That(dateValue, Is.EqualTo("Date"));
        Assert.That(amountValue, Is.EqualTo("Amount"));
    }

    [Test]
    public async Task Csv_ManualMapping_UpdatesSelectedColumn()
    {
        var csvDate = DateTime.UtcNow.Date.AddDays(-2);
        var csv = $"Date,Amount,Memo,Counterparty\n{ToIsoDate(csvDate)},-12.50,Manual mapped text,Store";

        await _importPage.NavigateAsync();
        await _importPage.SelectAccountAsync(_accountId);
        await _importPage.UploadFileContentAsync("manual-mapping.csv", "text/csv", csv);
        await _importPage.ClickPreviewAsync();

        await _importPage.MapColumnAsync("Memo", "Description");
        var descriptionValue = await Page.InputValueAsync("xpath=//label[contains(normalize-space(),'Beschrijving kolom')]/following-sibling::select[1]");
        Assert.That(descriptionValue, Is.EqualTo("Memo"));
    }

    [Test]
    public async Task Import_ExactDeduplication_SkipsDuplicateExternalId()
    {
        var firstDate = DateTime.UtcNow.Date.AddDays(-2);
        var secondDate = firstDate.AddDays(1);

        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedTransactionAsync(context, _accountId, -12.50m, firstDate, "Existing Coffee");
            var existing = await context.Transactions.FirstAsync(t => t.AccountId == _accountId);
            existing.ExternalId = "EXT-DUP";
            await context.SaveChangesAsync();
        }

        var csv = $"Date,Amount,Description,Counterparty,ExternalId\n{ToIsoDate(firstDate)},-12.50,Coffee duplicate,Store,EXT-DUP\n{ToIsoDate(secondDate)},-20.00,Lunch,Cafe,EXT-NEW";

        await _importPage.NavigateAsync();
        await _importPage.SelectAccountAsync(_accountId);
        await _importPage.UploadFileContentAsync("dedup-exact.csv", "text/csv", csv);
        await _importPage.ClickPreviewAsync();
        await _importPage.SelectDeduplicationModeAsync("Exact");
        await _importPage.ClickImportAsync();

        var resultText = await _importPage.GetImportResultAsync();
        Assert.That(resultText, Does.Contain("1 duplicaten overgeslagen"));
    }

    [Test]
    public async Task Import_FuzzyDeduplication_SkipsSimilarTransaction()
    {
        var seedDate = DateTime.UtcNow.Date.AddDays(-2);
        var importDate = seedDate.AddDays(1);

        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedDataHelper.SeedTransactionAsync(context, _accountId, -50.00m, seedDate, "Grocery Store Albert Heijn");
        }

        var csv = $"Date,Amount,Description,Counterparty,ExternalId\n{ToIsoDate(importDate)},-50.00,Grocery Store Heijn,Albert Heijn,EXT-FUZZY-1";

        await _importPage.NavigateAsync();
        await _importPage.SelectAccountAsync(_accountId);
        await _importPage.UploadFileContentAsync("dedup-fuzzy.csv", "text/csv", csv);
        await _importPage.ClickPreviewAsync();
        await _importPage.SelectDeduplicationModeAsync("Fuzzy");
        await _importPage.ClickImportAsync();

        var resultText = await _importPage.GetImportResultAsync();
        Assert.That(resultText, Does.Contain("1 duplicaten overgeslagen"));
    }

    [Test]
    public async Task Import_WithInvalidRows_ImportsValidSubset()
    {
        var validDate = DateTime.UtcNow.Date.AddDays(-2);
        var csv = $"Date,Amount,Description,Counterparty,ExternalId\nNOT_A_DATE,-12.50,Bad row,Store,EXT-BAD\n{ToIsoDate(validDate)},-20.00,Good row,Cafe,EXT-GOOD";

        await _importPage.NavigateAsync();
        await _importPage.SelectAccountAsync(_accountId);
        await _importPage.UploadFileContentAsync("partial.csv", "text/csv", csv);
        await _importPage.ClickPreviewAsync();

        Assert.That(await _importPage.GetPreviewCountAsync(), Is.EqualTo(1));

        await _importPage.SetSkipErrorsAsync(true);
        await _importPage.ClickImportAsync();

        var resultText = await _importPage.GetImportResultAsync();
        Assert.That(resultText, Does.Contain("1 transactie(s) geïmporteerd"));
    }

    [Test]
    public async Task Import_JsonFormat_Succeeds()
    {
        var firstDate = DateTime.UtcNow.Date.AddDays(-2);
        var secondDate = firstDate.AddDays(1);
        var json = $"[{{\"Date\":\"{ToIsoDate(firstDate)}\",\"Amount\":-5.25,\"Description\":\"Coffee JSON\",\"Counterparty\":\"Cafe\",\"ExternalId\":\"JSON-1\"}},{{\"Date\":\"{ToIsoDate(secondDate)}\",\"Amount\":42.0,\"Description\":\"Refund JSON\",\"Counterparty\":\"Shop\",\"ExternalId\":\"JSON-2\"}}]";

        await _importPage.NavigateAsync();
        await _importPage.SelectAccountAsync(_accountId);
        await _importPage.SelectFileFormatAsync("json");
        await _importPage.UploadFileContentAsync("import.json", "application/json", json);
        await _importPage.ClickPreviewAsync();
        await _importPage.ClickImportAsync();

        var resultText = await _importPage.GetImportResultAsync();
        Assert.That(resultText, Does.Contain("2 transactie(s) geïmporteerd"));
    }

    [Test]
    public async Task Import_NavigateToTransactions_ShowsImportedRow()
    {
        var csvDate = DateTime.UtcNow.Date.AddDays(-1);
        var csv = $"Date,Amount,Description,Counterparty,ExternalId\n{ToIsoDate(csvDate)},-35.00,US10 NAV TEST,Store,EXT-NAV";

        await _importPage.NavigateAsync();
        await _importPage.SelectAccountAsync(_accountId);
        await _importPage.UploadFileContentAsync("navigate.csv", "text/csv", csv);
        await _importPage.ClickPreviewAsync();
        await _importPage.ClickImportAsync();

        await Page.ClickAsync("a:has-text('Ga naar transacties')");
        await Page.WaitForURLAsync("**/transactions");

        Assert.That(Page.Url, Does.Contain("/transactions"));
        Assert.That(await Page.Locator("table[data-testid='transactions-table']").TextContentAsync(), Does.Contain("US10 NAV TEST"));
    }
}
