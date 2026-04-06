using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Unit;

/// <summary>
/// Unit tests for BreadcrumbService.
/// </summary>
[TestFixture]
public class BreadcrumbServiceTests
{
    // Simple NavigationManager stub for unit testing.
    private sealed class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager(string uri = "http://localhost/")
        {
            Initialize("http://localhost/", uri);
        }

        public void NavigateTo(string uri)
        {
            var absolute = uri.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? uri
                : "http://localhost" + uri;
            NotifyLocationChanged(absolute, false);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NotifyLocationChanged(uri, options.ForceLoad);
        }

        private void NotifyLocationChanged(string uri, bool forceLoad)
        {
            Uri = uri;
            NotifyLocationChanged(isInterceptedLink: false);
        }
    }

    private FakeNavigationManager _nav = null!;
    private BreadcrumbService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _nav = new FakeNavigationManager();
        _sut = new BreadcrumbService(_nav);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }

    [Test]
    public void RootUrl_ProducesHomeBreadcrumb()
    {
        // Already at "http://localhost/" from SetUp
        var items = _sut.Items;

        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].Text, Is.EqualTo("Home"));
        Assert.That(items[0].Href, Is.Null);
    }

    [Test]
    [TestCase("/accounts", "Rekeningen")]
    [TestCase("/budgets", "Budgetplannen")]
    [TestCase("/transactions", "Transacties")]
    [TestCase("/admin", "Beheer")]
    [TestCase("/sharing", "Delen")]
    [TestCase("/onboarding", "Onboarding")]
    public void KnownSegment_MapsToCorrectLabel(string path, string expectedLabel)
    {
        _nav = new FakeNavigationManager("http://localhost" + path);
        _sut.Dispose();
        _sut = new BreadcrumbService(_nav);

        var items = _sut.Items;

        // Last item must have the mapped label and no href (it's the current page)
        Assert.That(items.Last().Text, Is.EqualTo(expectedLabel));
        Assert.That(items.Last().Href, Is.Null);
    }

    [Test]
    public void GuidSegment_WithoutRegisteredTitle_FallsBackToDetails()
    {
        var id = Guid.NewGuid();
        _nav = new FakeNavigationManager($"http://localhost/accounts/{id}");
        _sut.Dispose();
        _sut = new BreadcrumbService(_nav);

        var items = _sut.Items;

        Assert.That(items.Last().Text, Is.EqualTo("Details"));
    }

    [Test]
    public void SetSectionTitle_RegistersTitle_ForGuidSegment()
    {
        var id = Guid.NewGuid();
        _nav = new FakeNavigationManager($"http://localhost/accounts/{id}");
        _sut.Dispose();
        _sut = new BreadcrumbService(_nav);

        _sut.SetSectionTitle(id.ToString(), "Betaalrekening");

        Assert.That(_sut.Items.Last().Text, Is.EqualTo("Betaalrekening"));
    }

    [Test]
    public void SetSectionTitle_FiresOnChangeEvent()
    {
        var id = Guid.NewGuid();
        _nav = new FakeNavigationManager($"http://localhost/accounts/{id}");
        _sut.Dispose();
        _sut = new BreadcrumbService(_nav);

        var fired = false;
        _sut.OnChange += () => fired = true;

        _sut.SetSectionTitle(id.ToString(), "Spaarrekening");

        Assert.That(fired, Is.True);
    }

    [Test]
    public void Navigation_ClearsRegisteredTitles()
    {
        var id = Guid.NewGuid();
        _nav = new FakeNavigationManager($"http://localhost/accounts/{id}");
        _sut.Dispose();
        _sut = new BreadcrumbService(_nav);

        _sut.SetSectionTitle(id.ToString(), "MijnRekening");
        Assert.That(_sut.Items.Last().Text, Is.EqualTo("MijnRekening"));

        // Navigate away and back — title should be cleared
        _nav.NavigateTo("/accounts");
        _nav.NavigateTo($"/accounts/{id}");

        Assert.That(_sut.Items.Last().Text, Is.EqualTo("Details"));
    }

    [Test]
    public void Navigation_FiresOnChangeEvent()
    {
        var fired = false;
        _sut.OnChange += () => fired = true;

        _nav.NavigateTo("/accounts");

        Assert.That(fired, Is.True);
    }

    [Test]
    public void UnknownSegment_Capitalised()
    {
        _nav = new FakeNavigationManager("http://localhost/foobar");
        _sut.Dispose();
        _sut = new BreadcrumbService(_nav);

        Assert.That(_sut.Items.Last().Text, Is.EqualTo("Foobar"));
    }

    [Test]
    public void NestedPath_BuildsCorrectHierarchy()
    {
        _nav = new FakeNavigationManager("http://localhost/admin/monitoring");
        _sut.Dispose();
        _sut = new BreadcrumbService(_nav);

        var items = _sut.Items.ToList();

        // Home → Beheer (with href) → Bewaking (no href)
        Assert.That(items, Has.Count.EqualTo(3));
        Assert.That(items[0].Text, Is.EqualTo("Home"));
        Assert.That(items[0].Href, Is.EqualTo("/"));
        Assert.That(items[1].Text, Is.EqualTo("Beheer"));
        Assert.That(items[1].Href, Is.EqualTo("/admin"));
        Assert.That(items[2].Text, Is.EqualTo("Bewaking"));
        Assert.That(items[2].Href, Is.Null);
    }

    [Test]
    public void Dispose_UnsubscribesFromLocationChanged()
    {
        _sut.Dispose();

        var fired = false;
        _sut.OnChange += () => fired = true;

        // Navigating after dispose should NOT trigger OnChange
        _nav.NavigateTo("/accounts");

        Assert.That(fired, Is.False);
    }
}
