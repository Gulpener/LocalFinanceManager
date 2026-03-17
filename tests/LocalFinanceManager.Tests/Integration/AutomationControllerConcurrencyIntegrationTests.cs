using FluentValidation;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Controllers;
using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs.ML;
using LocalFinanceManager.DTOs.Validators;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.Services.Background;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LocalFinanceManager.Tests.Integration;

[TestFixture]
public class AutomationControllerConcurrencyIntegrationTests
{
    private static readonly Guid TestUserId = TestUserContext.DefaultUserId;
    private SqliteConnection _connection = null!;
    private DbContextOptions<AppDbContext> _options = null!;

    [SetUp]
    public async Task Setup()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var setupContext = new AppDbContext(_options);
        await setupContext.Database.EnsureCreatedAsync();

        setupContext.AppSettings.Add(new AppSettings
        {
            UserId = TestUserId,
            AutoApplyEnabled = true,
            MinimumConfidence = 0.61f,
            IntervalMinutes = 15,
            AccountIdsJson = null,
            ExcludedCategoryIdsJson = null,
            UpdatedBy = "seed",
            IsArchived = false
        });

        await setupContext.SaveChangesAsync();
        await setupContext.Database.ExecuteSqlRawAsync(
            "UPDATE \"AppSettings\" SET \"XMin\" = 1 WHERE \"UserId\" = {0};",
            TestUserId);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task UpdateSettings_WhenConcurrentWriteOccurs_ReturnsConflictWithCurrentState()
    {
        var concurrentUpdateExecuted = false;

        async Task ForceConcurrentUpdateAsync()
        {
            if (concurrentUpdateExecuted)
            {
                return;
            }

            concurrentUpdateExecuted = true;

            await using var competingContext = new AppDbContext(_options);
            await competingContext.Database.ExecuteSqlRawAsync(
                """
                UPDATE "AppSettings"
                SET "AutoApplyEnabled" = 0,
                    "MinimumConfidence" = 0.33,
                    "IntervalMinutes" = 45,
                    "UpdatedBy" = 'concurrent-writer',
                    "UpdatedAt" = datetime('now'),
                    "XMin" = 2
                WHERE "UserId" = {0};
                """,
                TestUserId);
        }

        await using var staleContext = new ConcurrencyInjectingAppDbContext(_options, ForceConcurrentUpdateAsync);

        var undoService = new Mock<IUndoService>().Object;
        var monitoringService = new Mock<IMonitoringService>().Object;
        var options = Options.Create(new AutomationOptions
        {
            AutoApplyEnabled = false,
            ConfidenceThreshold = 0.85m
        });
        var settingsProvider = new Mock<IAutoApplySettingsProvider>();
        IValidator<AutoApplySettingsDto> validator = new AutoApplySettingsValidator();
        var logger = new Mock<ILogger<AutomationController>>().Object;
        var jobService = new Mock<IAutoApplyJobService>().Object;

        var controller = new AutomationController(
            undoService,
            monitoringService,
            jobService,
            staleContext,
            options,
            validator,
            settingsProvider.Object,
            new TestUserContext(TestUserId),
            logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.UpdateSettings(new AutoApplySettingsDto
        {
            Enabled = true,
            MinimumConfidence = 0.90f,
            IntervalMinutes = 10,
            AccountIds = new List<Guid>(),
            ExcludedCategoryIds = new List<Guid>()
        });

        Assert.That(result, Is.InstanceOf<ObjectResult>());

        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));

        var payload = objectResult.Value;
        Assert.That(payload, Is.Not.Null);

        var payloadType = payload!.GetType();
        var title = payloadType.GetProperty("title")?.GetValue(payload);
        var status = payloadType.GetProperty("status")?.GetValue(payload);
        var detail = payloadType.GetProperty("detail")?.GetValue(payload);
        var currentState = payloadType.GetProperty("currentState")?.GetValue(payload);

        Assert.That(title, Is.EqualTo("Concurrency conflict"));
        Assert.That(status, Is.EqualTo(409));
        Assert.That(detail, Is.EqualTo("The settings were modified by another user. Please reload and try again."));
        Assert.That(currentState, Is.InstanceOf<AutoApplySettingsDto>());

        var dto = (AutoApplySettingsDto)currentState!;
        Assert.That(dto.Enabled, Is.False);
        Assert.That(dto.MinimumConfidence, Is.EqualTo(0.33f).Within(0.001f));
        Assert.That(dto.IntervalMinutes, Is.EqualTo(45));

        settingsProvider.Verify(p => p.Invalidate(TestUserId), Times.Never);
    }

    private sealed class ConcurrencyInjectingAppDbContext : AppDbContext
    {
        private readonly Func<Task> _beforeFirstSaveAsync;
        private bool _alreadyInjected;

        public ConcurrencyInjectingAppDbContext(
            DbContextOptions<AppDbContext> options,
            Func<Task> beforeFirstSaveAsync)
            : base(options)
        {
            _beforeFirstSaveAsync = beforeFirstSaveAsync;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!_alreadyInjected && ChangeTracker.Entries<AppSettings>().Any(e => e.State == EntityState.Modified))
            {
                _alreadyInjected = true;
                await _beforeFirstSaveAsync();
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
