using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace LocalFinanceManager.E2E;

/// <summary>
/// Base class for E2E tests with screenshot/video capture on failure.
/// Automatically captures screenshots on test failure and videos in CI environment.
/// Uses one TestWebApplicationFactory per worker for parallel execution.
/// </summary>
[TestFixture]
public abstract class E2ETestBase : PageTest
{
    protected TestWebApplicationFactory? Factory;
    protected string BaseUrl => Factory?.ServerAddress ?? "http://localhost:5000";
    private string? _currentTestName;
    private readonly string _fixtureId = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Sets up the factory once per worker (test fixture).
    /// Creates a single TestWebApplicationFactory instance shared across all tests in this worker.
    /// </summary>
    [OneTimeSetUp]
    public async Task WorkerSetUp()
    {
        try
        {
            // Use fixture-specific ID for isolation (each test fixture gets its own server + database)
            Console.WriteLine($"[WorkerSetUp] Initializing fixture {_fixtureId}");
            TestContext.Out.WriteLine($"Initializing fixture {_fixtureId}");

            // Create factory with unique database per fixture
            Factory = new TestWebApplicationFactory(_fixtureId);
            Console.WriteLine($"[WorkerSetUp] Factory created with database: {Factory.TestDatabasePath}");
            Console.WriteLine($"[WorkerSetUp] Server will listen on: {Factory.ServerAddress}");

            // Delete old database files before starting server
            await Factory.InitializeDatabaseAsync();
            Console.WriteLine("[WorkerSetUp] Database files cleaned up");

            // Start the server (migrations will run automatically via Program.cs)
            Factory.EnsureServerStarted();
            Console.WriteLine("[WorkerSetUp] Factory.EnsureServerStarted() completed");

            // Wait for server to be ready (includes migration time)
            await WaitForServerReadyAsync();
            Console.WriteLine("[WorkerSetUp] Server is ready");

            // Now that migrations are complete, enable WAL mode for better concurrency
            await Factory.EnableWALModeAsync();
            Console.WriteLine("[WorkerSetUp] WAL mode enabled");

            TestContext.Out.WriteLine($"Fixture {_fixtureId} ready at: {BaseUrl}");
            Console.WriteLine($"[WorkerSetUp] Fixture {_fixtureId} ready at: {BaseUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WorkerSetUp] ERROR: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[WorkerSetUp] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Tears down the factory once per worker.
    /// Closes all connections and disposes the factory.
    /// </summary>
    [OneTimeTearDown]
    public async Task WorkerTearDown()
    {
        if (Factory != null)
        {
            await Factory.CloseAllConnectionsAsync();
            await Factory.DisposeAsync();
            Factory = null;
        }
    }

    /// <summary>
    /// Sets up each test.
    /// Optionally truncate tables here if tests need isolation.
    /// </summary>
    [SetUp]
    public async Task BaseSetUp()
    {
        _currentTestName = TestContext.CurrentContext.Test.Name;
        TestContext.Out.WriteLine($"Starting test: {_currentTestName}");

        // Optional: Uncomment if your tests need clean database state
        // Most tests can share state within a worker for better performance
        // await Factory!.TruncateTablesAsync();
    }

    /// <summary>
    /// Waits for the test server to be ready to accept HTTP connections.
    /// Polls the root endpoint with retries.
    /// </summary>
    private async Task WaitForServerReadyAsync()
    {
        if (Factory == null)
        {
            throw new InvalidOperationException("Factory is null - cannot check server readiness");
        }

        var serverUrl = Factory.ServerAddress;
        TestContext.Out.WriteLine($"Waiting for server at: {serverUrl}");

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var maxAttempts = 20; // Server needs time for migrations to run
        var delayMs = 500;

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(serverUrl);
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

        throw new InvalidOperationException($"Server failed to start after {maxAttempts} attempts ({maxAttempts * delayMs / 1000}s) at {serverUrl}");
    }

    /// <summary>
    /// Tears down each test.
    /// Captures screenshot on failure.
    /// </summary>
    [TearDown]
    public async Task BaseTearDown()
    {
        // Capture screenshot on test failure
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            await CaptureFailureScreenshotAsync();
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

        // Note: SlowMo is no longer supported in BrowserNewContextOptions.
        // To enable slow motion for debugging, configure BrowserTypeLaunchOptions.SlowMo
        // when launching the browser (e.g., in your Playwright/browser launch setup).

        return options;
    }
}
