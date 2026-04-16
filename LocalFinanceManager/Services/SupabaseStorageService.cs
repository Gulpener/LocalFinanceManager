using LocalFinanceManager.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LocalFinanceManager.Services;

/// <summary>
/// Implements file upload, deletion, and public URL generation via the Supabase Storage REST API.
/// </summary>
public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly SupabaseOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SupabaseStorageService> _logger;

    public SupabaseStorageService(
        IOptions<SupabaseOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<SupabaseStorageService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        string bucket,
        string path,
        Stream fileStream,
        string contentType,
        string userJwt,
        CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_options.Url.TrimEnd('/')}/storage/v1/object/{bucket}/{path}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userJwt);
        request.Headers.Add("apikey", _options.AnonKey);
        request.Headers.Add("x-upsert", "false");

        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = streamContent;

        using var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Supabase Storage upload failed for path {Path}: {StatusCode} {Body}", path, response.StatusCode, body);
            throw new InvalidOperationException($"Storage upload failed: {response.StatusCode}");
        }

        return path;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string bucket, string path, string userJwt, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_options.Url.TrimEnd('/')}/storage/v1/object/delete/{bucket}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userJwt);
        request.Headers.Add("apikey", _options.AnonKey);

        var body = JsonSerializer.Serialize(new { prefixes = new[] { path } });
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Supabase Storage delete failed for path {Path}: {StatusCode} {Body}", path, response.StatusCode, responseBody);
            throw new InvalidOperationException($"Storage delete failed: {response.StatusCode}");
        }
    }

    /// <inheritdoc />
    public string GetPublicUrl(string bucket, string path)
        => $"{_options.Url.TrimEnd('/')}/storage/v1/object/public/{bucket}/{path}";
}
