using Microsoft.Playwright.NUnit;

namespace LocalFinanceManager.E2E.Categories;

[TestFixture]
[Ignore("E2E tests require the application to be running. Run manually with: dotnet run (in LocalFinanceManager folder) then dotnet test (in this folder). Remove [Ignore] attribute when testing.")]
public class CategoryCrudTests : PageTest
{
    private string BaseUrl => "http://localhost:5244";

    [SetUp]
    public async Task Setup()
    {
        // Navigate to the categories page before each test
        await Page.GotoAsync($"{BaseUrl}/categories");
        await Page.WaitForLoadStateAsync();
    }

    [Test]
    public async Task CreateCategory_ValidData_Success()
    {
        // Click "Nieuwe Categorie" button
        await Page.ClickAsync("text=Nieuwe Categorie");
        await Page.WaitForURLAsync($"{BaseUrl}/categories/new");

        // Fill in the form
        await Page.FillAsync("#name", "Test Categorie E2E");

        // Submit the form
        await Page.ClickAsync("button[type=submit]");

        // Wait for navigation back to list
        await Page.WaitForURLAsync($"{BaseUrl}/categories");

        // Verify the category appears in the list
        await Expect(Page.Locator("text=Test Categorie E2E")).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditCategory_ValidData_Success()
    {
        // First create a category to edit
        await Page.ClickAsync("text=Nieuwe Categorie");
        await Page.FillAsync("#name", "Te Bewerken Categorie");
        await Page.ClickAsync("button[type=submit]");
        await Page.WaitForURLAsync($"{BaseUrl}/categories");

        // Find and click the edit button for the category
        var row = Page.Locator("tr:has-text('Te Bewerken Categorie')");
        await row.Locator("text=Bewerken").ClickAsync();

        // Wait for edit page
        await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex("/categories/.+/edit"));

        // Update the name
        await Page.FillAsync("#name", "Bijgewerkte Categorie");

        // Submit the form
        await Page.ClickAsync("button[type=submit]");

        // Wait for navigation back to list
        await Page.WaitForURLAsync($"{BaseUrl}/categories");

        // Verify the updated name appears in the list
        await Expect(Page.Locator("text=Bijgewerkte Categorie")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Te Bewerken Categorie")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task ArchiveCategory_ConfirmDialog_Success()
    {
        // First create a category to archive
        await Page.ClickAsync("text=Nieuwe Categorie");
        await Page.FillAsync("#name", "Te Archiveren Categorie");
        await Page.ClickAsync("button[type=submit]");
        await Page.WaitForURLAsync($"{BaseUrl}/categories");

        // Find and click the archive button for the category
        var row = Page.Locator("tr:has-text('Te Archiveren Categorie')");
        await row.Locator("text=Archiveren").ClickAsync();

        // Wait for confirmation dialog
        await Expect(Page.Locator(".modal-title:has-text('Bevestig Archivering')")).ToBeVisibleAsync();

        // Confirm archiving
        await Page.Locator(".modal-footer button.btn-danger:has-text('Archiveren')").ClickAsync();

        // Wait a moment for the archiving to complete
        await Page.WaitForTimeoutAsync(500);

        // Verify the category is no longer in the list
        await Expect(Page.Locator("text=Te Archiveren Categorie")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task ArchiveCategory_CancelDialog_CategoryRemainsInList()
    {
        // First create a category
        await Page.ClickAsync("text=Nieuwe Categorie");
        await Page.FillAsync("#name", "Niet Te Archiveren");
        await Page.ClickAsync("button[type=submit]");
        await Page.WaitForURLAsync($"{BaseUrl}/categories");

        // Find and click the archive button
        var row = Page.Locator("tr:has-text('Niet Te Archiveren')");
        await row.Locator("text=Archiveren").ClickAsync();

        // Wait for confirmation dialog
        await Expect(Page.Locator(".modal-title:has-text('Bevestig Archivering')")).ToBeVisibleAsync();

        // Cancel archiving
        await Page.Locator(".modal-footer button.btn-secondary:has-text('Annuleren')").ClickAsync();

        // Wait a moment for dialog to close
        await Page.WaitForTimeoutAsync(500);

        // Verify the category is still in the list
        await Expect(Page.Locator("text=Niet Te Archiveren")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreateCategory_EmptyName_ShowsValidationError()
    {
        // Click "Nieuwe Categorie" button
        await Page.ClickAsync("text=Nieuwe Categorie");
        await Page.WaitForURLAsync($"{BaseUrl}/categories/new");

        // Leave name empty and submit
        await Page.ClickAsync("button[type=submit]");

        // Verify we're still on the create page (validation failed)
        await Expect(Page).ToHaveURLAsync($"{BaseUrl}/categories/new");

        // Note: Validation error message check depends on FluentValidation client-side setup
        // If server-side only, we might see an error alert instead
    }

    [Test]
    public async Task NavigateToCategories_FromNavMenu_Success()
    {
        // Start from home page
        await Page.GotoAsync($"{BaseUrl}/");

        // Click on Categories nav link
        await Page.ClickAsync("nav a:has-text('Categorieën')");

        // Verify we're on the categories page
        await Page.WaitForURLAsync($"{BaseUrl}/categories");
        await Expect(Page.Locator("h1:has-text('Categorieën')")).ToBeVisibleAsync();
    }
}
