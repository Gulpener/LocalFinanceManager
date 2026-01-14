using Microsoft.Playwright.NUnit;

namespace LocalFinanceManager.E2E.Infrastructure;

/// <summary>
/// Smoke test to verify E2E test infrastructure is working.
/// </summary>
[TestFixture]
public class SmokeTests : PageTest
{
    [Test]
    public async Task Application_ShouldStart()
    {
        // This is a placeholder smoke test
        // Will be expanded with actual E2E scenarios in MVP-1+
        Assert.Pass("E2E test infrastructure ready");
    }
}
