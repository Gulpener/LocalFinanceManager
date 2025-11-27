using LocalFinanceManager.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// LocalFinanceManager Service Registrations
// TODO: Register Application Services
// builder.Services.AddScoped<ITransactionService, TransactionService>();
// builder.Services.AddScoped<IBudgetService, BudgetService>();
// builder.Services.AddScoped<ICategoryService, CategoryService>();
// builder.Services.AddScoped<IScoringEngine, ScoringEngine>();
// builder.Services.AddScoped<IRuleEngine, RuleEngine>();

// TODO: Register Infrastructure Services (Repositories)
// builder.Services.AddScoped<IAccountRepository, EfAccountRepository>();
// builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();
// builder.Services.AddScoped<ICategoryRepository, EfCategoryRepository>();

// TODO: Register DbContext
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
