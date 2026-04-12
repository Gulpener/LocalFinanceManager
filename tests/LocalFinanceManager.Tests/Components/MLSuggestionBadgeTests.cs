using Bunit;
using Bunit.JSInterop;
using LocalFinanceManager.Components.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http;

namespace LocalFinanceManager.Tests.Components;

[TestFixture]
public class MLSuggestionBadgeTests
{
    [Test]
    public void NonSuccessSuggestionResponse_ShowsNoModelBadgeFallback()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)))
        {
            BaseAddress = new Uri("http://localhost")
        };

        context.Services.AddSingleton(httpClient);
        context.Services.AddSingleton<ILogger<MLSuggestionBadge>>(NullLogger<MLSuggestionBadge>.Instance);

        var cut = context.Render<MLSuggestionBadge>(parameters => parameters
            .Add(p => p.TransactionId, Guid.NewGuid()));

        cut.WaitForAssertion(() =>
        {
            var noModelBadge = cut.Find("[data-testid='no-model-badge']");
            Assert.That(noModelBadge.TextContent, Does.Contain("Geen ML-model"));
        });
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
