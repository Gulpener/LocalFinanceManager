using LocalFinanceManager.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// -----------------------------------------------------------------------------
// DI Registrations Placeholder — add your services below
// -----------------------------------------------------------------------------
// Example service registrations (uncomment when implemented):
// builder.Services.AddScoped<ITransactionService, TransactionService>();
// builder.Services.AddScoped<ICategoryService, CategoryService>();
// builder.Services.AddScoped<IAccountRepository, EfAccountRepository>();
// builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlite("Data Source=App_Data/local.db"));
// -----------------------------------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
