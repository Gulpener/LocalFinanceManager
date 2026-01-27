using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.DTOs.Validators;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using LocalFinanceManager.Services.Background;
using LocalFinanceManager.Services.Import;
using FluentValidation;
using IbanNet;

namespace LocalFinanceManager.Extensions;

/// <summary>
/// Extension methods for configuring LocalFinanceManager services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all data access repositories (generic and specialized).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        // Generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        // Specialized repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBudgetPlanRepository, BudgetPlanRepository>();
        services.AddScoped<IBudgetLineRepository, BudgetLineRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionSplitRepository, TransactionSplitRepository>();
        services.AddScoped<ITransactionAuditRepository, TransactionAuditRepository>();
        services.AddScoped<ILabeledExampleRepository, LabeledExampleRepository>();
        
        return services;
    }

    /// <summary>
    /// Registers core domain services (Account, Category, BudgetPlan, TransactionAssignment).
    /// Depends on repositories being registered via AddDataAccess.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<BudgetPlanService>();
        services.AddScoped<ITransactionAssignmentService, TransactionAssignmentService>();
        
        return services;
    }

    /// <summary>
    /// Registers caching services (cache key tracker and budget/account lookup service).
    /// Depends on MemoryCache being configured in Program.cs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        services.AddSingleton<ICacheKeyTracker, CacheKeyTracker>();
        services.AddScoped<IBudgetAccountLookupService, BudgetAccountLookupService>();
        
        return services;
    }

    /// <summary>
    /// Registers ML.NET feature extraction and prediction services.
    /// Depends on repositories (for labeled examples) and MLOptions configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="includeBackgroundServices">Whether to register background ML retraining service (default: true).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMLServices(this IServiceCollection services, bool includeBackgroundServices = true)
    {
        services.AddScoped<ML.IFeatureExtractor, ML.FeatureExtractor>();
        services.AddScoped<ML.IMLService, MLService>();
        
        if (includeBackgroundServices)
        {
            services.AddHostedService<MLRetrainingBackgroundService>();
        }
        
        return services;
    }

    /// <summary>
    /// Registers automation services (monitoring, undo) and auto-apply background worker.
    /// Depends on repositories, ML services, and AutomationOptions configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="includeBackgroundServices">Whether to register auto-apply background service (default: true).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutomationServices(this IServiceCollection services, bool includeBackgroundServices = true)
    {
        services.AddScoped<IMonitoringService, MonitoringService>();
        services.AddScoped<IUndoService, UndoService>();
        
        if (includeBackgroundServices)
        {
            services.AddHostedService<AutoApplyBackgroundService>();
        }
        
        return services;
    }

    /// <summary>
    /// Registers import services (CSV/JSON parsers, matching strategies, import orchestration).
    /// Depends on repositories and ImportOptions configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddImportServices(this IServiceCollection services)
    {
        services.AddScoped<CsvImportParser>();
        services.AddScoped<JsonImportParser>();
        services.AddScoped<ExactMatchStrategy>();
        services.AddScoped<FuzzyMatchStrategy>();
        services.AddScoped<ImportService>();
        
        return services;
    }

    /// <summary>
    /// Registers FluentValidation validators for all DTOs and IbanNet validator.
    /// No dependencies - can be called at any point in the registration sequence.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        // FluentValidation validators
        services.AddScoped<IValidator<CreateAccountRequest>, CreateAccountRequestValidator>();
        services.AddScoped<IValidator<UpdateAccountRequest>, UpdateAccountRequestValidator>();
        services.AddScoped<IValidator<CreateCategoryDto>, CreateCategoryDtoValidator>();
        services.AddScoped<IValidator<CreateBudgetPlanDto>, CreateBudgetPlanDtoValidator>();
        services.AddScoped<IValidator<UpdateBudgetPlanDto>, UpdateBudgetPlanDtoValidator>();
        services.AddScoped<IValidator<CreateBudgetLineDto>, CreateBudgetLineDtoValidator>();
        services.AddScoped<IValidator<UpdateBudgetLineDto>, UpdateBudgetLineDtoValidator>();
        services.AddScoped<IValidator<AssignTransactionRequest>, AssignTransactionRequestValidator>();
        services.AddScoped<IValidator<SplitTransactionRequest>, SplitTransactionRequestValidator>();
        services.AddScoped<IValidator<BulkAssignTransactionsRequest>, BulkAssignTransactionsRequestValidator>();
        services.AddScoped<IValidator<UndoAssignmentRequest>, UndoAssignmentRequestValidator>();
        
        // IbanNet validator
        services.AddSingleton<IIbanValidator, IbanValidator>();
        
        return services;
    }
}
