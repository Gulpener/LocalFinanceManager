using LocalFinanceManager.Controllers;
using LocalFinanceManager.DTOs.ML;
using LocalFinanceManager.Services;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Controllers;

[TestFixture]
public class AutomationControllerTests
{
    private AppDbContext _dbContext = null!;
    private AutomationController _controller = null!;
    private IUndoService _undoService = null!;
    private IMonitoringService _monitoringService = null!;
    private IOptions<AutomationOptions> _automationOptions = null!;
    private ILogger<AutomationController> _logger = null!;

    [SetUp]
    public void Setup()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        // Mock services (simplified for settings tests)
        _undoService = NSubstitute.Substitute.For<IUndoService>();
        _monitoringService = NSubstitute.Substitute.For<IMonitoringService>();
        _automationOptions = Options.Create(new AutomationOptions
        {
            AutoApplyEnabled = false,
            ConfidenceThreshold = 0.85m
        });
        _logger = NSubstitute.Substitute.For<ILogger<AutomationController>>();

        _controller = new AutomationController(
            _undoService,
            _monitoringService,
            _dbContext,
            _automationOptions,
            _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.CloseConnection();
        _dbContext.Dispose();
    }

    [Test]
    public async Task GetSettings_NoDbSettings_ReturnsFallbackFromAppsettings()
    {
        // Act
        var result = await _controller.GetSettings();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var settings = okResult.Value as AutoApplySettingsDto;
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.Enabled, Is.False);
        Assert.That(settings.MinimumConfidence, Is.EqualTo(0.85f));
        Assert.That(settings.IntervalMinutes, Is.EqualTo(15));
    }

    [Test]
    public async Task UpdateSettings_ValidSettings_SavesToDatabase()
    {
        // Arrange
        var newSettings = new AutoApplySettingsDto
        {
            Enabled = true,
            MinimumConfidence = 0.75f,
            IntervalMinutes = 30,
            AccountIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            ExcludedCategoryIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var result = await _controller.UpdateSettings(newSettings);

        // Assert - Verify API response
        Assert.That(result, Is.InstanceOf<OkObjectResult>());

        // Verify database persistence
        var dbSettings = await _dbContext.AppSettings.FindAsync(1);
        Assert.That(dbSettings, Is.Not.Null);
        Assert.That(dbSettings!.AutoApplyEnabled, Is.True);
        Assert.That(dbSettings.MinimumConfidence, Is.EqualTo(0.75f));
        Assert.That(dbSettings.IntervalMinutes, Is.EqualTo(30));
        Assert.That(dbSettings.AccountIdsJson, Is.Not.Null);
        Assert.That(dbSettings.ExcludedCategoryIdsJson, Is.Not.Null);
    }

    [Test]
    public async Task UpdateSettings_ThenGetSettings_ReturnsPersistedValues()
    {
        // Arrange
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();
        var categoryId1 = Guid.NewGuid();

        var newSettings = new AutoApplySettingsDto
        {
            Enabled = true,
            MinimumConfidence = 0.80f,
            IntervalMinutes = 20,
            AccountIds = new List<Guid> { accountId1, accountId2 },
            ExcludedCategoryIds = new List<Guid> { categoryId1 }
        };

        // Act - Save settings
        await _controller.UpdateSettings(newSettings);

        // Act - Retrieve settings
        var result = await _controller.GetSettings();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var settings = okResult.Value as AutoApplySettingsDto;
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.Enabled, Is.True);
        Assert.That(settings.MinimumConfidence, Is.EqualTo(0.80f));
        Assert.That(settings.IntervalMinutes, Is.EqualTo(20));
        Assert.That(settings.AccountIds, Has.Count.EqualTo(2));
        Assert.That(settings.AccountIds, Contains.Item(accountId1));
        Assert.That(settings.AccountIds, Contains.Item(accountId2));
        Assert.That(settings.ExcludedCategoryIds, Has.Count.EqualTo(1));
        Assert.That(settings.ExcludedCategoryIds, Contains.Item(categoryId1));
    }

    [Test]
    public async Task UpdateSettings_InvalidConfidence_ReturnsBadRequest()
    {
        // Arrange
        var invalidSettings = new AutoApplySettingsDto
        {
            Enabled = true,
            MinimumConfidence = 1.5f, // Invalid: > 1.0
            IntervalMinutes = 15,
            AccountIds = new List<Guid>(),
            ExcludedCategoryIds = new List<Guid>()
        };

        // Act
        var result = await _controller.UpdateSettings(invalidSettings);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateSettings_NullSettings_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UpdateSettings(null!);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateSettings_EmptyAccountsAndCategories_SavesAsNull()
    {
        // Arrange
        var newSettings = new AutoApplySettingsDto
        {
            Enabled = true,
            MinimumConfidence = 0.85f,
            IntervalMinutes = 15,
            AccountIds = new List<Guid>(), // Empty = all accounts
            ExcludedCategoryIds = new List<Guid>() // Empty = no exclusions
        };

        // Act
        await _controller.UpdateSettings(newSettings);

        // Assert
        var dbSettings = await _dbContext.AppSettings.FindAsync(1);
        Assert.That(dbSettings, Is.Not.Null);
        Assert.That(dbSettings!.AccountIdsJson, Is.Null);
        Assert.That(dbSettings.ExcludedCategoryIdsJson, Is.Null);
    }
}
