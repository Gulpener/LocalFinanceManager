using LocalFinanceManager.Configuration;
using LocalFinanceManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Tests.Unit;

[TestFixture]
public class SupabaseStorageServiceTests
{
    [Test]
    public async Task UploadAsync_UsesPostAndTrimmedSupabaseUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });
        var client = new HttpClient(handler);
        var factory = new StubHttpClientFactory(client);

        var service = new SupabaseStorageService(
            Options.Create(new SupabaseOptions { Url = "https://example.supabase.co/", AnonKey = "anon" }),
            factory,
            NullLogger<SupabaseStorageService>.Instance);

        await service.UploadAsync("profile-pictures", "user/avatar.png", new MemoryStream([0x89, 0x50, 0x4E, 0x47]), "image/png", "jwt");

        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(capturedRequest.RequestUri!.ToString(),
            Is.EqualTo("https://example.supabase.co/storage/v1/object/profile-pictures/user/avatar.png"));
    }

    [Test]
    public void GetPublicUrl_TrimsTrailingSlash()
    {
        var service = new SupabaseStorageService(
            Options.Create(new SupabaseOptions { Url = "https://example.supabase.co/", AnonKey = "anon" }),
            new StubHttpClientFactory(new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)))),
            NullLogger<SupabaseStorageService>.Instance);

        var url = service.GetPublicUrl("profile-pictures", "user/avatar.png");

        Assert.That(url, Is.EqualTo("https://example.supabase.co/storage/v1/object/public/profile-pictures/user/avatar.png"));
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
