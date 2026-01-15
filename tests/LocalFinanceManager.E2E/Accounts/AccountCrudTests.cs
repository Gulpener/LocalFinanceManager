using Microsoft.Playwright.NUnit;

namespace LocalFinanceManager.E2E.Accounts;

[TestFixture]
[Ignore("E2E tests require the application to be running. Run manually after starting the server.")]
public class AccountCrudTests : PageTest
{
    private string BaseUrl => "https://localhost:5001";

    [Test]
    public async Task CreateAccount_ValidData_Success()
    {
        // This test is disabled until we have a proper test harness set up
        await Task.CompletedTask;
    }
}
