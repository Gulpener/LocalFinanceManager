using Bunit;
using LocalFinanceManager.Components.Shared;
using Microsoft.AspNetCore.Components;

namespace LocalFinanceManager.Tests.Components;

[TestFixture]
public class ShortcutHelpTests
{
    [Test]
    public void HelpModal_Displays_AllKeyboardShortcuts()
    {
        using var context = new BunitContext();
        var cut = context.Render<ShortcutHelp>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsTouchDevice, false));

        var text = cut.Markup;

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Tab"));
            Assert.That(text, Does.Contain("Enter"));
            Assert.That(text, Does.Contain("Esc"));
            Assert.That(text, Does.Contain("Space"));
            Assert.That(text, Does.Contain("Ctrl"));
            Assert.That(text, Does.Contain("/"));
            Assert.That(text, Does.Contain("?"));
            Assert.That(text, Does.Contain("↑"));
            Assert.That(text, Does.Contain("Home"));
            Assert.That(text, Does.Contain("End"));
        });
    }

    [Test]
    public void HelpModal_Closes_When_EscapePressed()
    {
        using var context = new BunitContext();
        var closed = false;

        var cut = context.Render<ShortcutHelp>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closed = true)));

        cut.Find("#shortcutHelpModal").KeyDown("Escape");

        Assert.That(closed, Is.True);
    }

    [Test]
    public void HelpModal_Shows_TouchSection_OnTouchDevices()
    {
        using var context = new BunitContext();
        var cut = context.Render<ShortcutHelp>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsTouchDevice, true));

        var text = cut.Markup;

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Touch gebaren"));
            Assert.That(text, Does.Contain("Tik"));
            Assert.That(text, Does.Contain("Swipe"));
            Assert.That(text, Does.Contain("Lang indrukken"));
        });
    }
}
