namespace LocalFinanceManager.Services;

/// <summary>
/// Item in a breadcrumb trail.
/// </summary>
/// <param name="Text">Display text.</param>
/// <param name="Href">Optional navigation URL. Null for the current (last) item.</param>
public record BreadcrumbItem(string Text, string? Href = null);

/// <summary>
/// Scoped service that auto-generates a breadcrumb trail from the current URL.
/// Pages with UUID segments should call <see cref="SetSectionTitle"/> during
/// <c>OnInitializedAsync</c> to supply a human-readable name for the segment.
/// </summary>
public interface IBreadcrumbService
{
    /// <summary>Current breadcrumb trail derived from the active URL.</summary>
    IReadOnlyList<BreadcrumbItem> Items { get; }

    /// <summary>Raised whenever the trail changes (navigation or manual title override).</summary>
    event Action? OnChange;

    /// <summary>
    /// Registers a human-readable title for a URL segment (usually a GUID).
    /// Rebuilds the trail and raises <see cref="OnChange"/>.
    /// </summary>
    void SetSectionTitle(string segmentOrId, string title);
}
