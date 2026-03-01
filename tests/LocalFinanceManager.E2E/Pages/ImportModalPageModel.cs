using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Page Object Model for the transaction import flow page.
/// Encapsulates upload, preview, mapping and execute import interactions.
/// </summary>
public class ImportModalPageModel : PageObjectBase
{
    private const string AccountSelectSelector = "#accountSelect";
    private const string FileFormatSelector = "#fileFormat";
    private const string FileInputSelector = "#fileInput";
    private const string PreviewButtonSelector = "button:has-text('Volgende: Preview')";
    private const string PreviewStepHeaderSelector = "h5:has-text('Stap 2: Controleer kolom-mapping')";
    private const string PreviewCountHeaderSelector = "h6:has-text('Voorbeeld transacties')";
    private const string SkipErrorsSelector = "#skipErrors";
    private const string ExecuteImportButtonSelector = "button:has-text('Importeer')";
    private const string ResultHeaderSelector = "h5:has-text('Import resultaat')";

    public ImportModalPageModel(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    public async Task NavigateAsync()
    {
        await NavigateToAsync("/transactions/import");
    }

    public async Task SelectAccountAsync(Guid accountId)
    {
        await Page.SelectOptionAsync(AccountSelectSelector, accountId.ToString());
    }

    public async Task SelectFileFormatAsync(string format)
    {
        await Page.SelectOptionAsync(FileFormatSelector, format);
    }

    public async Task UploadFileAsync(string filePath)
    {
        await Page.SetInputFilesAsync(FileInputSelector, filePath);
    }

    public async Task UploadFileContentAsync(string fileName, string mimeType, string content)
    {
        await Page.SetInputFilesAsync(FileInputSelector, new FilePayload
        {
            Name = fileName,
            MimeType = mimeType,
            Buffer = System.Text.Encoding.UTF8.GetBytes(content)
        });
    }

    public async Task ClickPreviewAsync()
    {
        await Page.ClickAsync(PreviewButtonSelector);
        await WaitForSelectorAsync(PreviewStepHeaderSelector, 10000);
    }

    public async Task<bool> IsPreviewVisibleAsync()
    {
        var previewHeader = await Page.QuerySelectorAsync(PreviewStepHeaderSelector);
        return previewHeader != null && await previewHeader.IsVisibleAsync();
    }

    public async Task<int> GetPreviewCountAsync()
    {
        var text = await Page.Locator(PreviewCountHeaderSelector).First.TextContentAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var match = System.Text.RegularExpressions.Regex.Match(text, @"van\s+(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
        {
            return count;
        }

        return 0;
    }

    public async Task MapColumnAsync(string columnName, string targetField)
    {
        var field = targetField.Trim().ToLowerInvariant();
        var selector = field switch
        {
            "date" => "xpath=//label[contains(normalize-space(),'Datum kolom')]/following-sibling::select[1]",
            "amount" => "xpath=//label[contains(normalize-space(),'Bedrag kolom')]/following-sibling::select[1]",
            "description" => "xpath=//label[contains(normalize-space(),'Beschrijving kolom')]/following-sibling::select[1]",
            "counterparty" => "xpath=//label[contains(normalize-space(),'Tegenpartij kolom')]/following-sibling::select[1]",
            "externalid" => "xpath=//label[contains(normalize-space(),'Externe ID kolom')]/following-sibling::select[1]",
            _ => throw new ArgumentOutOfRangeException(nameof(targetField), $"Unknown mapping field: {targetField}")
        };

        await Page.SelectOptionAsync(selector, columnName);
    }

    public async Task SelectDeduplicationModeAsync(string mode)
    {
        var value = mode.Trim().ToLowerInvariant() switch
        {
            "exact" => "exact",
            "fuzzy" => "fuzzy",
            "none" => "none",
            _ => "exact"
        };

        await Page.SelectOptionAsync("xpath=//label[contains(normalize-space(),'Deduplicatie strategie')]/following-sibling::select[1]", value);
    }

    public async Task SetSkipErrorsAsync(bool skipErrors)
    {
        var checkbox = Page.Locator(SkipErrorsSelector).First;
        if (skipErrors)
        {
            await checkbox.CheckAsync();
        }
        else
        {
            await checkbox.UncheckAsync();
        }
    }

    public async Task ClickImportAsync()
    {
        await Page.ClickAsync(ExecuteImportButtonSelector);
        await WaitForSelectorAsync(ResultHeaderSelector, 10000);
    }

    public async Task<string> GetImportResultAsync()
    {
        var successText = await Page.Locator(".alert-success").First.TextContentAsync();
        if (!string.IsNullOrWhiteSpace(successText))
        {
            return successText;
        }

        var errorText = await Page.Locator(".alert-danger").First.TextContentAsync();
        return errorText ?? string.Empty;
    }
}
