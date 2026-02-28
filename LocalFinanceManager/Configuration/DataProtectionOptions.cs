namespace LocalFinanceManager.Configuration;

/// <summary>
/// Configuration options for ASP.NET Core Data Protection key management.
/// </summary>
public class DataProtectionOptions
{
    public const string SectionName = "DataProtection";

    /// <summary>
    /// Filesystem path for persisted Data Protection keys.
    /// Can be absolute or relative to the application content root.
    /// </summary>
    public string KeyRingPath { get; set; } = "data-protection-keys";

    /// <summary>
    /// Shared application name used to isolate Data Protection payloads.
    /// Must remain stable across deployments to keep existing payloads decryptable.
    /// </summary>
    public string ApplicationName { get; set; } = "LocalFinanceManager";
}
