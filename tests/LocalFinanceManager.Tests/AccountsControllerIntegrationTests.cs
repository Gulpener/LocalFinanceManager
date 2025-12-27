using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LocalFinanceManager.Core;
using Xunit;

namespace LocalFinanceManager.Tests
{
    public class CustomWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        }
    }

    public class AccountsControllerIntegrationTests : IClassFixture<CustomWebAppFactory>
    {
        private readonly CustomWebAppFactory _factory;

        public AccountsControllerIntegrationTests(CustomWebAppFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async System.Threading.Tasks.Task PostGetAndArchiveAccount()
        {
            var client = _factory.CreateClient();

            var create = new
            {
                label = "IntegrationTestAccount",
                type = 0,
                currency = "EUR",
                iban = "NL00INTEG0000000000",
                startingBalance = 10.0m
            };

            var post = await client.PostAsJsonAsync("api/accounts", create);
            Assert.True(post.IsSuccessStatusCode, await post.Content.ReadAsStringAsync());

            var list = await client.GetFromJsonAsync<JsonElement[]>("api/accounts");
            Assert.NotNull(list);
            Assert.True(list!.Any(item => item.GetProperty("label").GetString() == "IntegrationTestAccount"));

            string id = null!;
            foreach (var it in list!)
            {
                if (it.GetProperty("label").GetString() == "IntegrationTestAccount") { id = it.GetProperty("id").GetString()!; break; }
            }
            Assert.False(string.IsNullOrEmpty(id));

            var del = await client.DeleteAsync($"api/accounts/{id}");
            Assert.True(del.IsSuccessStatusCode || del.StatusCode == System.Net.HttpStatusCode.NoContent, await del.Content.ReadAsStringAsync());

            var archivedList = await client.GetFromJsonAsync<JsonElement[]>("api/accounts?includeArchived=true");
            Assert.NotNull(archivedList);
            Assert.True(archivedList!.Any(item => item.GetProperty("label").GetString() == "IntegrationTestAccount" && item.GetProperty("isArchived").GetBoolean() == true));
        }
    }
}
