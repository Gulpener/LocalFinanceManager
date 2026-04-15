namespace LocalFinanceManager.Services;

/// <summary>
/// Service for managing files in Supabase Storage.
/// </summary>
public interface ISupabaseStorageService
{
    /// <summary>Uploads a file to Supabase Storage and returns the storage path.</summary>
    Task<string> UploadAsync(string bucket, string path, Stream fileStream, string contentType, string userJwt, CancellationToken ct = default);

    /// <summary>Deletes a file from Supabase Storage.</summary>
    Task DeleteAsync(string bucket, string path, string userJwt, CancellationToken ct = default);

    /// <summary>Returns the public URL for a stored file.</summary>
    string GetPublicUrl(string bucket, string path);
}
