using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Page Object Model for the Bulk Assign Modal.
/// Provides methods to monitor progress, success/failure counts, and error details during bulk assignment.
/// </summary>
public class BulkAssignModalPageModel : PageObjectBase
{
    // Selectors
    private const string ModalSelector = "[data-testid='bulk-assign-modal']";
    private const string ProgressBarSelector = "[data-testid='bulk-progress-bar']";
    private const string SuccessCountSelector = "[data-testid='success-count']";
    private const string FailureCountSelector = "[data-testid='failure-count']";
    private const string ErrorAccordionSelector = "[data-testid='error-accordion']";
    private const string CloseButtonSelector = "button[data-action='close-bulk-modal']";
    private const string ProgressPercentageSelector = "[data-testid='progress-percentage']";

    /// <summary>
    /// Initializes a new instance of the BulkAssignModalPageModel class.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <param name="baseUrl">Base URL for the application.</param>
    public BulkAssignModalPageModel(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    /// <summary>
    /// Waits for the bulk assign modal to appear.
    /// </summary>
    public async Task WaitForModalAsync()
    {
        await WaitForSelectorAsync(ModalSelector);
    }

    /// <summary>
    /// Gets the current progress percentage (0-100).
    /// </summary>
    /// <returns>Progress percentage as an integer.</returns>
    public async Task<int> GetProgressPercentageAsync()
    {
        var progressElement = await Page.QuerySelectorAsync(ProgressPercentageSelector);
        if (progressElement == null)
        {
            return 0;
        }

        var text = await progressElement.TextContentAsync();
        var percentageText = text?.Replace("%", "").Trim();

        if (int.TryParse(percentageText, out var percentage))
        {
            return percentage;
        }

        return 0;
    }

    /// <summary>
    /// Gets the success count (number of successfully assigned transactions).
    /// </summary>
    /// <returns>Success count as an integer.</returns>
    public async Task<int> GetSuccessCountAsync()
    {
        var successElement = await Page.QuerySelectorAsync(SuccessCountSelector);
        if (successElement == null)
        {
            return 0;
        }

        var text = await successElement.TextContentAsync();

        if (int.TryParse(text?.Trim(), out var count))
        {
            return count;
        }

        return 0;
    }

    /// <summary>
    /// Gets the failure count (number of failed transaction assignments).
    /// </summary>
    /// <returns>Failure count as an integer.</returns>
    public async Task<int> GetFailureCountAsync()
    {
        var failureElement = await Page.QuerySelectorAsync(FailureCountSelector);
        if (failureElement == null)
        {
            return 0;
        }

        var text = await failureElement.TextContentAsync();

        if (int.TryParse(text?.Trim(), out var count))
        {
            return count;
        }

        return 0;
    }

    /// <summary>
    /// Expands the error details accordion to view failure details.
    /// </summary>
    public async Task ExpandErrorDetailsAsync()
    {
        await Page.ClickAsync(ErrorAccordionSelector);
        // Wait for accordion content to be visible after animation
        var errorContent = Page.Locator("[data-testid='error-message']").First;
        await errorContent.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 2000 });
    }

    /// <summary>
    /// Gets the error messages from the error accordion.
    /// </summary>
    /// <returns>List of error message texts.</returns>
    /// <remarks>
    /// Call ExpandErrorDetailsAsync() first to ensure accordion is expanded.
    /// </remarks>
    public async Task<List<string>> GetErrorMessagesAsync()
    {
        var errorElements = await Page.QuerySelectorAllAsync("[data-testid='error-message']");

        var tasks = errorElements.Select(async element =>
        {
            var text = await element.TextContentAsync();
            return text?.Trim();
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(text => !string.IsNullOrWhiteSpace(text)).ToList()!;
    }

    /// <summary>
    /// Clicks the Close button to dismiss the bulk assign modal.
    /// </summary>
    public async Task ClickCloseAsync()
    {
        await Page.ClickAsync(CloseButtonSelector);
        await WaitForModalToCloseAsync();
    }

    /// <summary>
    /// Waits for the bulk operation to complete (progress reaches 100%).
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 30000ms).</param>
    public async Task WaitForCompletionAsync(int timeoutMs = 30000)
    {
        // Wait for progress bar to reach 100% using Playwright's built-in wait
        var progressBar = Page.Locator(ProgressBarSelector);
        await progressBar.WaitForAsync(new() 
        { 
            Timeout = timeoutMs,
            State = WaitForSelectorState.Attached 
        });
        
        // Wait for aria-valuenow attribute to equal 100
        await Page.WaitForFunctionAsync(
            @"() => {
                const progressBar = document.querySelector('.progress-bar');
                return progressBar && parseInt(progressBar.getAttribute('aria-valuenow')) >= 100;
            }",
            new PageWaitForFunctionOptions { Timeout = timeoutMs });
    }

    /// <summary>
    /// Checks if the modal is currently visible.
    /// </summary>
    /// <returns>True if modal is visible, false otherwise.</returns>
    public async Task<bool> IsModalVisibleAsync()
    {
        var modal = await Page.QuerySelectorAsync(ModalSelector);
        return modal != null && await modal.IsVisibleAsync();
    }

    /// <summary>
    /// Waits for the modal to close (disappear from DOM).
    /// </summary>
    private async Task WaitForModalToCloseAsync()
    {
        await Page.WaitForSelectorAsync(ModalSelector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 5000
        });
    }
}
