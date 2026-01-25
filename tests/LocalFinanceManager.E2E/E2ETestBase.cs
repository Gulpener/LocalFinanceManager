using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace LocalFinanceManager.E2E;

/// <summary>
/// Base class for E2E tests with screenshot/video capture on failure.
/// Automatically captures screenshots on test failure and videos in CI environment.
/// </summary>
[TestFixture]
public abstract class E2ETestBase : PageTest
{
    protected TestWebApplicationFactory? Factory;
    protected string BaseUrl => Factory?.ServerAddress ?? "http://localhost:5000";
    private string? _currentTestName;

    /// <summary>
    /// Sets up the test fixture before each test.
    /// Creates a new TestWebApplicationFactory instance and starts the test server.
    /// </summary>
    [SetUp]
    public async Task BaseSetUp()
    {
        _currentTestName = TestContext.CurrentContext.Test.Name;

        // Create factory
        Factory = new TestWebApplicationFactory();

        // Start the Kestrel server
        await Factory.StartServerAsync();

        // Server should be ready now (StartServerAsync waits for full initialization)
        TestContext.Out.WriteLine($"Connecting to server at: {BaseUrl}");
    }

    /// <summary>
    /// Waits for the test server to be ready to accept HTTP connections.
    /// Polls the root endpoint with retries.
    /// </summary>
    private async Task WaitForServerReadyAsync()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var maxAttempts = 20; // Server needs time for migrations to run
        var delayMs = 500;

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(BaseUrl);
                // Server responded (even if error response), so it's ready
                TestContext.Out.WriteLine($"Server ready after {i + 1} attempts (status: {response.StatusCode})");
                return;
            }
            catch (HttpRequestException ex)
            {
                // Server not ready yet, wait and retry
                if (i == 0 || i == 5 || i == 10 || i == maxAttempts - 1)
                {
                    TestContext.Out.WriteLine($"Attempt {i + 1}/{maxAttempts}: {ex.Message}");
                }
                if (i < maxAttempts - 1)
                {
                    await Task.Delay(delayMs);
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout, server not ready yet
                if (i == 0 || i == 5 || i == 10 || i == maxAttempts - 1)
                {
                    TestContext.Out.WriteLine($"Attempt {i + 1}/{maxAttempts}: Timeout");
                }
                if (i < maxAttempts - 1)
                {
                    await Task.Delay(delayMs);
                }
            }
        }

        throw new InvalidOperationException($"Server failed to start after {maxAttempts} attempts ({maxAttempts * delayMs / 1000}s) at {BaseUrl}");
    }

    /// <summary>
    /// Tears down the test fixture after each test.
    /// Captures screenshot on failure and cleans up resources.
    /// </summary>
    [TearDown]
    public async Task BaseTearDown()
    {
        try
        {
            // Capture screenshot on test failure
            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                await CaptureFailureScreenshotAsync();
            }
        }
        finally
        {
            // Dispose factory to clean up test database
            if (Factory != null)
            {
                await Factory.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Captures a screenshot on test failure for debugging.
    /// Saves screenshot to test-results/screenshots/ directory with timestamp.
    /// </summary>
    private async Task CaptureFailureScreenshotAsync()
    {
        if (Page == null)
        {
            return;
        }

        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var sanitizedTestName = SanitizeFileName(_currentTestName ?? "UnknownTest");
            var fileName = $"{sanitizedTestName}_{timestamp}.png";
            var screenshotPath = Path.Combine("test-results", "screenshots", fileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });

            TestContext.Out.WriteLine($"Screenshot captured: {screenshotPath}");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Failed to capture screenshot: {ex.Message}");
        }
    }

    /// <summary>
    /// Sanitizes a file name by removing invalid characters.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized;
    }

    /// <summary>
    /// Configures browser context options for the test.
    /// Override this method to customize browser behavior (e.g., viewport size, locale).
    /// </summary>
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();

        // Enable video recording in CI environment
        var isCIEnvironment = Environment.GetEnvironmentVariable("CI")?.ToLower() == "true";
        if (isCIEnvironment)
        {
            options.RecordVideoDir = Path.Combine("test-results", "videos");
            options.RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 };
        }

        // Set default viewport size
        options.ViewportSize = new ViewportSize { Width = 1920, Height = 1080 };

        // Note: SlowMo is no longer supported in BrowserNewContextOptions
        // Use Playwright launch options instead if slow motion is needed

        return options;
    }
}
