using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LocalFinanceManager.Tests.Infrastructure;

[TestFixture]
public class DevSmokeAuthenticationHandlerTests
{
    [Test]
    public async Task AuthenticateAsync_WhenEnableDevSmokeAuthIsFalse_ReturnsNoResult()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Dev-Smoke-UserId"] = Guid.NewGuid().ToString();
        httpContext.Request.Headers["X-Dev-Smoke-Sub"] = "00000000-0000-0000-0000-000000000001";
        httpContext.Request.Headers["X-Dev-Smoke-Email"] = "dev@localfinancemanager.local";

        var webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
        webHostEnvironmentMock.SetupGet(e => e.EnvironmentName).Returns("Development");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EnableDevSmokeAuth"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(webHostEnvironmentMock.Object);
        services.AddSingleton<IConfiguration>(configuration);
        httpContext.RequestServices = services.BuildServiceProvider();

        var optionsMonitorMock = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitorMock
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());

        using var loggerFactory = LoggerFactory.Create(_ => { });

        var handler = new DevSmokeAuthenticationHandler(
            optionsMonitorMock.Object,
            loggerFactory,
            UrlEncoder.Default);

        await handler.InitializeAsync(
            new AuthenticationScheme("DevSmoke", "DevSmoke", typeof(DevSmokeAuthenticationHandler)),
            httpContext);

        var result = await handler.AuthenticateAsync();

        Assert.That(result.None, Is.True);
    }
}
