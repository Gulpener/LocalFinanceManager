using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Pages;

/// <summary>
/// Base class for Page Object Models.
/// Provides common navigation, wait, and screenshot helpers for E2E tests.
/// </summary>
public abstract class PageObjectBase
{
    /// <summary>
    /// Playwright page instance for browser interactions.
    /// </summary>
    protected IPage Page { get; }

    /// <summary>
    /// Base URL for the application (e.g., "http://localhost:5000").
    /// </summary>
    protected string BaseUrl { get; }

    /// <summary>
    /// Initializes a new instance of the PageObjectBase class.
    /// </summary>
    /// <param name="page">Playwright page instance.</param>
    /// <param name="baseUrl">Base URL for the application.</param>
    protected PageObjectBase(IPage page, string baseUrl)
    {
        Page = page;
        BaseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Navigates to the specified path relative to the base URL.
    /// </summary>
    /// <param name="path">Relative path (e.g., "/accounts", "/transactions").</param>
    /// <param name="waitUntil">Wait condition (default: NetworkIdle).</param>
    public async Task NavigateToAsync(string path, WaitUntilState waitUntil = WaitUntilState.NetworkIdle)
    {
        var url = $"{BaseUrl}{path}";
        await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = waitUntil });
    }

    /// <summary>
    /// Waits for a selector to appear in the DOM and be visible.
    /// </summary>
    /// <param name="selector">CSS selector to wait for.</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000ms).</param>
    public async Task WaitForSelectorAsync(string selector, int timeoutMs = 5000)
    {
        await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
    }

    /// <summary>
    /// Takes a screenshot of the current page and saves it to the test-results/screenshots directory.
    /// </summary>
    /// <param name="fileName">File name for the screenshot (without extension).</param>
    public async Task TakeScreenshotAsync(string fileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var screenshotPath = Path.Combine("test-results", "screenshots", $"{fileName}_{timestamp}.png");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
    }

    /// <summary>
    /// Waits for navigation to complete after performing an action.
    /// </summary>
    /// <param name="action">Action that triggers navigation (e.g., clicking a button).</param>
    /// <param name="waitUntil">Wait condition (default: NetworkIdle).</param>
    /// <remarks>
    /// Note: Consider using Playwright's expect API (Page.Expect) for modern navigation handling.
    /// This method uses a legacy API for backward compatibility.
    /// </remarks>
    public async Task WaitForNavigationAsync(Func<Task> action, WaitUntilState waitUntil = WaitUntilState.NetworkIdle)
    {
        var navigationTask = Page.WaitForURLAsync("**", new PageWaitForURLOptions
        {
            WaitUntil = waitUntil
        });

        var actionTask = action();

        await Task.WhenAll(navigationTask, actionTask);
    }
}
