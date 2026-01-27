using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Extensions;
using LocalFinanceManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Tests.Helpers;

/// <summary>
/// Test helper that builds a full DI container with all application services registered.
/// Simplifies integration tests by eliminating manual service instantiation and mock setup.
/// </summary>
public sealed class TestServiceProvider : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private bool _disposed;

    /// <summary>
    /// Creates a new test service provider with the specified test database context.
    /// </summary>
    /// <param name="context">The test database context (in-memory SQLite).</param>
    /// <param name="loggerFactory">Optional logger factory for test debugging. Defaults to NullLoggerFactory.</param>
    public TestServiceProvider(AppDbContext context, ILoggerFactory? loggerFactory = null)
    {
        var services = new ServiceCollection();

        // Register the test database context as singleton (one instance per test)
        services.AddSingleton(context);

        // Register logger factory (defaults to null logger for performance)
        services.AddSingleton(loggerFactory ?? NullLoggerFactory.Instance);
        services.AddLogging();

        // Register test-friendly configuration options with empty/default values
        services.AddSingleton(Options.Create(new ImportOptions()));
        services.AddSingleton(Options.Create(new MLOptions()));
        services.AddSingleton(Options.Create(new AutomationOptions()));
        services.AddSingleton(Options.Create(new CacheOptions()));

        // Register MemoryCache (required by caching services)
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000;
            options.CompactionPercentage = 0.25;
        });

        // Register all application services using extension methods
        // Background services excluded (includeBackgroundServices: false) to prevent
        // auto-start interference with tests
        services.AddDataAccess();
        services.AddValidation();
        services.AddCachingServices();
        services.AddDomainServices();
        services.AddImportServices();
        services.AddMLServices(includeBackgroundServices: false);
        services.AddAutomationServices(includeBackgroundServices: false);

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets a required service from the DI container.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the service is not registered.</exception>
    public T GetService<T>() where T : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Creates a new service scope for tests that need explicit scope control.
    /// Caller is responsible for disposing the returned scope.
    /// </summary>
    /// <returns>A new service scope.</returns>
    public IServiceScope CreateScope()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _serviceProvider.CreateScope();
    }

    /// <summary>
    /// Disposes the service provider and all registered services.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _serviceProvider.Dispose();
            _disposed = true;
        }
    }
}
