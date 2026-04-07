namespace LocalFinanceManager.Models;

/// <summary>
/// Stores per-user UI preferences (theme, etc.).
/// Keyed on the inherited BaseEntity.UserId (local User.Id Guid).
/// </summary>
public class UserPreferences : BaseEntity
{
    /// <summary>"light" or "dark".</summary>
    public string Theme { get; set; } = "light";
}
