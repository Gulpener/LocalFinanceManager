using LocalFinanceManager.Controllers;
using LocalFinanceManager.DTOs.ML;
using LocalFinanceManager.DTOs.Validators;
using LocalFinanceManager.Services;
using LocalFinanceManager.Services.Background;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Http;
using FluentValidation;
using System.Text.Json;

namespace LocalFinanceManager.Tests.Controllers;

[TestFixture]
public class AutomationControllerTests
{
    private AppDbContext _dbContext = null!;
    private AutomationController _controller = null!;
    private IUndoService _undoService = null!;
    private IMonitoringService _monitoringService = null!;
    private IOptions<AutomationOptions> _automationOptions = null!;
    private Mock<IAutoApplySettingsProvider> _settingsProviderMock = null!;
    private ILogger<AutomationController> _logger = null!;
    private Guid _currentUserId;

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
        _undoService = new Mock<IUndoService>().Object;
        _monitoringService = new Mock<IMonitoringService>().Object;
        _automationOptions = Options.Create(new AutomationOptions
        {
            AutoApplyEnabled = false,
            ConfidenceThreshold = 0.85m
        });
        _settingsProviderMock = new Mock<IAutoApplySettingsProvider>();
        _logger = new Mock<ILogger<AutomationController>>().Object;
        var settingsValidator = new AutoApplySettingsValidator();
        var jobService = new Mock<IAutoApplyJobService>().Object;
        _currentUserId = Guid.NewGuid();
        var userContext = new TestUserContext(_currentUserId);

        _controller = new AutomationController(
            _undoService,
            _monitoringService,
            jobService,
            _dbContext,
            _automationOptions,
            settingsValidator,
            _settingsProviderMock.Object,
            userContext,
            _logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Setup validator with actual FluentValidation
        var objectValidator = new Mock<IObjectModelValidator>();
        objectValidator.Setup(o => o.Validate(
            It.IsAny<ActionContext>(),
            It.IsAny<ValidationStateDictionary>(),
            It.IsAny<string>(),
            It.IsAny<object>()))
        .Callback<ActionContext, ValidationStateDictionary, string, object>(
            (context, state, prefix, model) =>
            {
                if (model is AutoApplySettingsDto dto)
                {
                    var validationResult = settingsValidator.Validate(dto);
                    if (!validationResult.IsValid)
                    {
                        foreach (var error in validationResult.Errors)
                        {
                            context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                        }
                    }
                }
            });
        _controller.ObjectValidator = objectValidator.Object;
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
    public async Task GetSettings_CorruptedStoredJson_ReturnsOkWithEmptyLists()
    {
        // Arrange
        _dbContext.AppSettings.Add(new AppSettings
        {
            UserId = _currentUserId,
            AutoApplyEnabled = true,
            MinimumConfidence = 0.77f,
            IntervalMinutes = 25,
            AccountIdsJson = "{broken-json",
            ExcludedCategoryIdsJson = "invalid-json",
            UpdatedBy = "seed",
            IsArchived = false
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetSettings();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var settings = okResult.Value as AutoApplySettingsDto;

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.Enabled, Is.True);
        Assert.That(settings.MinimumConfidence, Is.EqualTo(0.77f));
        Assert.That(settings.IntervalMinutes, Is.EqualTo(25));
        Assert.That(settings.AccountIds, Is.Empty);
        Assert.That(settings.ExcludedCategoryIds, Is.Empty);
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
        var dbSettings = await _dbContext.AppSettings
            .FirstOrDefaultAsync(s => !s.IsArchived && s.UserId == _currentUserId);
        Assert.That(dbSettings, Is.Not.Null);
        Assert.That(dbSettings!.AutoApplyEnabled, Is.True);
        Assert.That(dbSettings.MinimumConfidence, Is.EqualTo(0.75f));
        Assert.That(dbSettings.IntervalMinutes, Is.EqualTo(30));
        Assert.That(dbSettings.AccountIdsJson, Is.Not.Null);
        Assert.That(dbSettings.ExcludedCategoryIdsJson, Is.Not.Null);
        _settingsProviderMock.Verify(provider => provider.Invalidate(_currentUserId), Times.Once);
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
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(objectResult.Value, Is.InstanceOf<ValidationProblemDetails>());

        var problemDetails = (ValidationProblemDetails)objectResult.Value!;
        Assert.That(problemDetails.Errors.ContainsKey(nameof(AutoApplySettingsDto.MinimumConfidence)), Is.True);
        Assert.That(
            problemDetails.Errors[nameof(AutoApplySettingsDto.MinimumConfidence)],
            Contains.Item("Minimum confidence must be between 0.0 and 1.0"));
    }

    [Test]
    public async Task UpdateSettings_NullSettings_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UpdateSettings(null!);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(objectResult.Value, Is.InstanceOf<ValidationProblemDetails>());

        var problemDetails = (ValidationProblemDetails)objectResult.Value!;
        Assert.That(problemDetails.Errors.ContainsKey("settings"), Is.True);
        Assert.That(problemDetails.Errors["settings"], Contains.Item("Settings payload is required."));
    }

    [Test]
    public async Task UpdateSettings_IntervalMinutesTooLow_ReturnsBadRequest()
    {
        // Arrange
        var invalidSettings = new AutoApplySettingsDto
        {
            Enabled = true,
            MinimumConfidence = 0.8f,
            IntervalMinutes = 0,
            AccountIds = new List<Guid>(),
            ExcludedCategoryIds = new List<Guid>()
        };

        // Act
        var result = await _controller.UpdateSettings(invalidSettings);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(objectResult.Value, Is.InstanceOf<ValidationProblemDetails>());

        var problemDetails = (ValidationProblemDetails)objectResult.Value!;
        Assert.That(problemDetails.Errors.ContainsKey(nameof(AutoApplySettingsDto.IntervalMinutes)), Is.True);
        Assert.That(
            problemDetails.Errors[nameof(AutoApplySettingsDto.IntervalMinutes)],
            Contains.Item("Interval must be greater than 0 minutes"));
    }

    [Test]
    public async Task UpdateSettings_IntervalMinutesNegative_ReturnsBadRequest()
    {
        // Arrange
        var invalidSettings = new AutoApplySettingsDto
        {
            Enabled = true,
            MinimumConfidence = 0.8f,
            IntervalMinutes = -1,
            AccountIds = new List<Guid>(),
            ExcludedCategoryIds = new List<Guid>()
        };

        // Act
        var result = await _controller.UpdateSettings(invalidSettings);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(objectResult.Value, Is.InstanceOf<ValidationProblemDetails>());

        var problemDetails = (ValidationProblemDetails)objectResult.Value!;
        Assert.That(problemDetails.Errors.ContainsKey(nameof(AutoApplySettingsDto.IntervalMinutes)), Is.True);
        Assert.That(
            problemDetails.Errors[nameof(AutoApplySettingsDto.IntervalMinutes)],
            Contains.Item("Interval must be greater than 0 minutes"));
    }

    [Test]
    public async Task UpdateSettings_IntervalMinutesTooHigh_ReturnsBadRequest()
    {
        // Arrange
        var invalidSettings = new AutoApplySettingsDto
        {
            Enabled = true,
            MinimumConfidence = 0.8f,
            IntervalMinutes = 1441,
            AccountIds = new List<Guid>(),
            ExcludedCategoryIds = new List<Guid>()
        };

        // Act
        var result = await _controller.UpdateSettings(invalidSettings);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(objectResult.Value, Is.InstanceOf<ValidationProblemDetails>());

        var problemDetails = (ValidationProblemDetails)objectResult.Value!;
        Assert.That(problemDetails.Errors.ContainsKey(nameof(AutoApplySettingsDto.IntervalMinutes)), Is.True);
        Assert.That(
            problemDetails.Errors[nameof(AutoApplySettingsDto.IntervalMinutes)],
            Contains.Item("Interval cannot exceed 1440 minutes (24 hours)"));
    }

    [Test]
    public async Task UpdateSettings_NullAccountIds_ReturnsBadRequest()
    {
        // Arrange
        var invalidSettings = new AutoApplySettingsDto
        {
            Enabled = true,
            MinimumConfidence = 0.8f,
            IntervalMinutes = 15,
            AccountIds = null!,
            ExcludedCategoryIds = new List<Guid>()
        };

        // Act
        var result = await _controller.UpdateSettings(invalidSettings);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(objectResult.Value, Is.InstanceOf<ValidationProblemDetails>());

        var problemDetails = (ValidationProblemDetails)objectResult.Value!;
        Assert.That(problemDetails.Errors.ContainsKey(nameof(AutoApplySettingsDto.AccountIds)), Is.True);
        Assert.That(
            problemDetails.Errors[nameof(AutoApplySettingsDto.AccountIds)],
            Contains.Item("AccountIds cannot be null"));
    }

    [Test]
    public async Task UpdateSettings_NullExcludedCategoryIds_ReturnsBadRequest()
    {
        // Arrange
        var invalidSettings = new AutoApplySettingsDto
        {
            Enabled = true,
            MinimumConfidence = 0.8f,
            IntervalMinutes = 15,
            AccountIds = new List<Guid>(),
            ExcludedCategoryIds = null!
        };

        // Act
        var result = await _controller.UpdateSettings(invalidSettings);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(objectResult.Value, Is.InstanceOf<ValidationProblemDetails>());

        var problemDetails = (ValidationProblemDetails)objectResult.Value!;
        Assert.That(problemDetails.Errors.ContainsKey(nameof(AutoApplySettingsDto.ExcludedCategoryIds)), Is.True);
        Assert.That(
            problemDetails.Errors[nameof(AutoApplySettingsDto.ExcludedCategoryIds)],
            Contains.Item("ExcludedCategoryIds cannot be null"));
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
        var dbSettings = await _dbContext.AppSettings
            .FirstOrDefaultAsync(s => !s.IsArchived && s.UserId == _currentUserId);
        Assert.That(dbSettings, Is.Not.Null);
        Assert.That(dbSettings!.AccountIdsJson, Is.Null);
        Assert.That(dbSettings.ExcludedCategoryIdsJson, Is.Null);
    }

    [Test]
    public async Task UpdateSettings_ConcurrencyConflict_WithCorruptedStoredJson_Returns409WithFallbackCurrentState()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var conflictDbContext = new ConcurrencyThrowingAppDbContext(options);
        conflictDbContext.Database.OpenConnection();
        conflictDbContext.Database.EnsureCreated();

        conflictDbContext.AppSettings.Add(new AppSettings
        {
            UserId = _currentUserId,
            AutoApplyEnabled = true,
            MinimumConfidence = 0.91f,
            IntervalMinutes = 33,
            AccountIdsJson = "{corrupted-json",
            ExcludedCategoryIdsJson = "not-json",
            UpdatedBy = "seed",
            IsArchived = false
        });
        conflictDbContext.SaveChanges();

        var settingsProviderMock = new Mock<IAutoApplySettingsProvider>();
        var jobService = new Mock<IAutoApplyJobService>().Object;
        var controller = new AutomationController(
            _undoService,
            _monitoringService,
            jobService,
            conflictDbContext,
            _automationOptions,
            new AutoApplySettingsValidator(),
            settingsProviderMock.Object,
            new TestUserContext(_currentUserId),
            _logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var updateRequest = new AutoApplySettingsDto
        {
            Enabled = false,
            MinimumConfidence = 0.75f,
            IntervalMinutes = 15,
            AccountIds = new List<Guid>(),
            ExcludedCategoryIds = new List<Guid>()
        };

        // Act
        var result = await controller.UpdateSettings(updateRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        var conflictResult = (ConflictObjectResult)result;
        Assert.That(conflictResult.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));

        var payloadJson = JsonSerializer.Serialize(conflictResult.Value);
        using var payload = JsonDocument.Parse(payloadJson);
        var root = payload.RootElement;

        Assert.That(root.GetProperty("status").GetInt32(), Is.EqualTo(StatusCodes.Status409Conflict));
        var currentState = root.GetProperty("currentState");

        Assert.That(currentState.GetProperty("Enabled").GetBoolean(), Is.True);
        Assert.That(currentState.GetProperty("MinimumConfidence").GetSingle(), Is.EqualTo(0.91f));
        Assert.That(currentState.GetProperty("IntervalMinutes").GetInt32(), Is.EqualTo(33));
        Assert.That(currentState.GetProperty("AccountIds").GetArrayLength(), Is.EqualTo(0));
        Assert.That(currentState.GetProperty("ExcludedCategoryIds").GetArrayLength(), Is.EqualTo(0));

        settingsProviderMock.Verify(provider => provider.Invalidate(_currentUserId), Times.Never);
    }

    private sealed class ConcurrencyThrowingAppDbContext : AppDbContext
    {
        public ConcurrencyThrowingAppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new DbUpdateConcurrencyException("Forced concurrency failure for test");
        }
    }
}
