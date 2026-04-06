using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Tests.Integration;

[TestFixture]
public class BudgetPlanServiceIntegrationTests
{
    private AppDbContext _context = null!;
    private TestServiceProvider _serviceProvider = null!;
    private BudgetPlanService _budgetPlanService = null!;
    private ICategoryRepository _categoryRepository = null!;
    private CategoryService _categoryService = null!;

    [SetUp]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _serviceProvider = new TestServiceProvider(_context);
        _budgetPlanService = _serviceProvider.GetService<BudgetPlanService>();
        _categoryRepository = _serviceProvider.GetService<ICategoryRepository>();
        _categoryService = _serviceProvider.GetService<CategoryService>();

        // Create a default test account
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = "Test Account",
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            Type = AccountType.Checking,
            StartingBalance = 1000m,
            IsArchived = false
        };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
        _context?.Database.CloseConnection();
        _context?.Dispose();
    }

    [Test]
    public async Task CreateAsync_WithPersonalTemplate_CreatesExpectedCategories()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync();
        var createDto = new CreateBudgetPlanDto
        {
            AccountId = account.Id,
            Year = 2026,
            Name = "Personal Budget 2026",
            TemplateName = "Personal"
        };

        // Act
        var result = await _budgetPlanService.CreateAsync(createDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Personal Budget 2026"));

        var categories = await _categoryRepository.GetByBudgetPlanAsync(result.Id);
        Assert.That(categories, Has.Count.EqualTo(6), "Personal template should create 6 categories");

        // Verify specific categories exist
        Assert.That(categories.Any(c => c.Name == "Salaris" && c.Type == CategoryType.Income), Is.True);
        Assert.That(categories.Any(c => c.Name == "Wonen" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Vervoer" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Eten & Drinken" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Vrije Tijd" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Sparen" && c.Type == CategoryType.Expense), Is.True);
    }

    [Test]
    public async Task CreateAsync_WithBusinessTemplate_CreatesExpectedCategories()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync();
        var createDto = new CreateBudgetPlanDto
        {
            AccountId = account.Id,
            Year = 2026,
            Name = "Business Budget 2026",
            TemplateName = "Business"
        };

        // Act
        var result = await _budgetPlanService.CreateAsync(createDto);

        // Assert
        var categories = await _categoryRepository.GetByBudgetPlanAsync(result.Id);
        Assert.That(categories, Has.Count.EqualTo(5), "Business template should create 5 categories");

        Assert.That(categories.Any(c => c.Name == "Omzet" && c.Type == CategoryType.Income), Is.True);
        Assert.That(categories.Any(c => c.Name == "Kostprijs Verkopen" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Bedrijfskosten" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Marketing" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Salarissen" && c.Type == CategoryType.Expense), Is.True);
    }

    [Test]
    public async Task CreateAsync_WithHouseholdTemplate_CreatesExpectedCategories()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync();
        var createDto = new CreateBudgetPlanDto
        {
            AccountId = account.Id,
            Year = 2026,
            Name = "Household Budget 2026",
            TemplateName = "Household"
        };

        // Act
        var result = await _budgetPlanService.CreateAsync(createDto);

        // Assert
        var categories = await _categoryRepository.GetByBudgetPlanAsync(result.Id);
        Assert.That(categories, Has.Count.EqualTo(6), "Household template should create 6 categories");

        Assert.That(categories.Any(c => c.Name == "Inkomsten" && c.Type == CategoryType.Income), Is.True);
        Assert.That(categories.Any(c => c.Name == "Huur/Hypotheek" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Nutsvoorzieningen" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Boodschappen" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Kinderopvang" && c.Type == CategoryType.Expense), Is.True);
        Assert.That(categories.Any(c => c.Name == "Zorgkosten" && c.Type == CategoryType.Expense), Is.True);
    }

    [Test]
    public async Task CreateAsync_WithEmptyTemplate_CreatesNoCategories()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync();
        var createDto = new CreateBudgetPlanDto
        {
            AccountId = account.Id,
            Year = 2026,
            Name = "Empty Budget 2026",
            TemplateName = "Empty"
        };

        // Act
        var result = await _budgetPlanService.CreateAsync(createDto);

        // Assert
        var categories = await _categoryRepository.GetByBudgetPlanAsync(result.Id);
        Assert.That(categories, Has.Count.EqualTo(0), "Empty template should create 0 categories");
    }

    [Test]
    public async Task CategoryEditability_AfterTemplateApplication_CategoriesAreFullyEditable()
    {
        // Arrange - Create budget plan with Personal template
        var account = await _context.Accounts.FirstAsync();
        var createDto = new CreateBudgetPlanDto
        {
            AccountId = account.Id,
            Year = 2026,
            Name = "Editable Budget 2026",
            TemplateName = "Personal"
        };

        var budgetPlan = await _budgetPlanService.CreateAsync(createDto);
        var categories = await _categoryRepository.GetByBudgetPlanAsync(budgetPlan.Id);

        // Find the "Salaris" category created by template
        var salaryCategory = categories.First(c => c.Name == "Salaris");

        // Act - Update the category name from "Salaris" to "Inkomsten"
        var updateDto = new UpdateCategoryDto
        {
            Name = "Income",
            Type = CategoryType.Income,
            XMin = salaryCategory.XMin
        };

        var updatedCategory = await _categoryService.UpdateAsync(salaryCategory.Id, updateDto);

        // Assert - Verify update succeeded
        Assert.That(updatedCategory, Is.Not.Null);
        Assert.That(updatedCategory!.Name, Is.EqualTo("Income"));
        Assert.That(updatedCategory.Type, Is.EqualTo(CategoryType.Income));

        // Verify in database
        var fromDb = await _categoryRepository.GetByIdAsync(salaryCategory.Id);
        Assert.That(fromDb, Is.Not.Null);
        Assert.That(fromDb!.Name, Is.EqualTo("Income"), "Category should be editable after template application");
    }

    [Test]
    public async Task CreateFromTemplateAsync_InvalidTemplate_ThrowsValidationException()
    {
        // Arrange
        var account = await _context.Accounts.FirstAsync();
        var createDto = new CreateBudgetPlanDto
        {
            AccountId = account.Id,
            Year = 2026,
            Name = "Invalid Template Budget",
            TemplateName = "InvalidTemplate"
        };

        // Act & Assert
        Assert.ThrowsAsync<FluentValidation.ValidationException>(async () =>
            await _budgetPlanService.CreateFromTemplateAsync(createDto, "InvalidTemplate"));
    }
}
