using Microsoft.AspNetCore.Components;

namespace LocalFinanceManager.Services;

/// <summary>
/// Scoped implementation of <see cref="IBreadcrumbService"/>.
/// Subscribes to <see cref="NavigationManager.LocationChanged"/> and rebuilds the
/// breadcrumb trail on every navigation event.
/// </summary>
public sealed class BreadcrumbService : IBreadcrumbService, IDisposable
{
    private static readonly Dictionary<string, string> SegmentLabels =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["accounts"] = "Accounts",
            ["budgets"] = "Budget Plans",
            ["transactions"] = "Transactions",
            ["admin"] = "Admin",
            ["sharing"] = "Sharing",
            ["backup"] = "Backup & Restore",
            ["onboarding"] = "Onboarding",
            ["new"] = "New",
            ["edit"] = "Edit",
            ["import"] = "Import",
            ["shared-with-me"] = "Shared with Me",
            ["categories"] = "Categories",
            ["monitoring"] = "Monitoring",
            ["ml"] = "ML Info",
            ["settings"] = "Settings",
            ["autoapply"] = "Auto-Apply",
        };

    private readonly NavigationManager _navigationManager;
    private readonly Dictionary<string, string> _registeredTitles = new(StringComparer.OrdinalIgnoreCase);

    private List<BreadcrumbItem> _items = new();

    public BreadcrumbService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
        _navigationManager.LocationChanged += OnLocationChanged;
        RebuildTrail();
    }

    /// <inheritdoc />
    public IReadOnlyList<BreadcrumbItem> Items => _items;

    /// <inheritdoc />
    public event Action? OnChange;

    /// <inheritdoc />
    public void SetSectionTitle(string segmentOrId, string title)
    {
        _registeredTitles[segmentOrId] = title;
        RebuildTrail();
        OnChange?.Invoke();
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        _registeredTitles.Clear();
        RebuildTrail();
        OnChange?.Invoke();
    }

    private void RebuildTrail()
    {
        var uri = _navigationManager.Uri;
        var path = new Uri(uri).AbsolutePath.TrimEnd('/');
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var items = new List<BreadcrumbItem>();

        if (segments.Length == 0)
        {
            items.Add(new BreadcrumbItem("Home"));
            _items = items;
            return;
        }

        items.Add(new BreadcrumbItem("Home", "/"));

        var cumulativePath = string.Empty;
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            cumulativePath += "/" + segment;
            bool isLast = i == segments.Length - 1;
            string? href = isLast ? null : cumulativePath;

            string label;
            if (Guid.TryParse(segment, out _))
            {
                label = _registeredTitles.TryGetValue(segment, out var registered)
                    ? registered
                    : "Details";
            }
            else if (SegmentLabels.TryGetValue(segment, out var mapped))
            {
                label = mapped;
            }
            else
            {
                label = segment.Length > 0
                    ? char.ToUpperInvariant(segment[0]) + segment[1..]
                    : segment;
            }

            items.Add(new BreadcrumbItem(label, href));
        }

        _items = items;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
