using System.ComponentModel.DataAnnotations;

namespace LocalFinanceManager.Models;

/// <summary>
/// Stores per-user UI preferences (theme, profile info, etc.).
/// Uses the inherited BaseEntity.Id as the primary key and is logically one-per-user via a unique UserId owner reference.
/// </summary>
public class UserPreferences : BaseEntity
{
    /// <summary>"light" or "dark".</summary>
    public string Theme { get; set; } = "light";

    /// <summary>User's first name for profile display.</summary>
    [MaxLength(100)]
    public string? FirstName { get; set; }

    /// <summary>User's last name for profile display.</summary>
    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>Path within the Supabase Storage bucket for the profile picture.</summary>
    public string? ProfileImagePath { get; set; }
}
