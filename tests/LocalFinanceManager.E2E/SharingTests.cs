using LocalFinanceManager.Data;
using LocalFinanceManager.E2E.Helpers;
using LocalFinanceManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E;

/// <summary>
/// E2E tests for the sharing system.
/// Uses the seed user (SeedUserId) as the share recipient and seeds a second user as the owner.
/// </summary>
[TestFixture]
public class SharingTests : E2ETestBase
{
    private static readonly Guid SecondUserId = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private const string SecondUserEmail = "sharer@e2etest.local";

    [SetUp]
    public async Task SetUp()
    {
        await Factory!.TruncateTablesAsync();

        using var scope = Factory.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure seed user exists (the authenticated user in E2E tests)
        if (!context.Users.Any(u => u.Id == AppDbContext.SeedUserId))
        {
            context.Users.Add(new User
            {
                Id = AppDbContext.SeedUserId,
                SupabaseUserId = AppDbContext.SeedUserId.ToString(),
                Email = AppDbContext.SeedUserEmail,
                DisplayName = "Dev User",
                EmailConfirmed = true
            });
        }

        // Create a second user who will share resources with the seed user
        if (!context.Users.Any(u => u.Id == SecondUserId))
        {
            context.Users.Add(new User
            {
                Id = SecondUserId,
                SupabaseUserId = SecondUserId.ToString(),
                Email = SecondUserEmail,
                DisplayName = "Sharer User",
                EmailConfirmed = true
            });
        }

        await context.SaveChangesAsync();
    }

    [Test]
    public async Task SharedWithMePage_ShowsPendingInvitation()
    {
        // Arrange: seed an account owned by SecondUser, share it with the seed user
        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Label = "Shared Account",
                Type = AccountType.Checking,
                IBAN = "NL91ABNA0417164300",
                Currency = "EUR",
                StartingBalance = 500,
                UserId = SecondUserId
            };
            context.Accounts.Add(account);
            await context.SaveChangesAsync();

            var share = new AccountShare
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                SharedWithUserId = AppDbContext.SeedUserId,
                Permission = PermissionLevel.Viewer,
                Status = ShareStatus.Pending,
                UserId = SecondUserId
            };
            context.AccountShares.Add(share);
            await context.SaveChangesAsync();
        }

        // Act: navigate to SharedWithMe page
        await Page.GotoAsync($"{BaseUrl}/sharing/shared-with-me", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        // Assert: pending invitation is visible
        await Expect(Page.GetByText("Openstaande uitnodigingen")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Shared Account")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Viewer")).ToBeVisibleAsync();
    }

    [Test]
    public async Task SharedWithMePage_AcceptShare_MovesToAccepted()
    {
        Guid shareId;

        // Arrange: seed account + pending share
        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Label = "Accept Test Account",
                Type = AccountType.Checking,
                IBAN = "NL91ABNA0417164300",
                Currency = "EUR",
                StartingBalance = 500,
                UserId = SecondUserId
            };
            context.Accounts.Add(account);
            await context.SaveChangesAsync();

            var share = new AccountShare
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                SharedWithUserId = AppDbContext.SeedUserId,
                Permission = PermissionLevel.Viewer,
                Status = ShareStatus.Pending,
                UserId = SecondUserId
            };
            context.AccountShares.Add(share);
            await context.SaveChangesAsync();
            shareId = share.Id;
        }

        // Act: navigate to SharedWithMe page
        await Page.GotoAsync($"{BaseUrl}/sharing/shared-with-me", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        // Click Accept button
        var acceptButton = Page.Locator("button:has-text('Accepteren')").First;
        await acceptButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        await acceptButton.ClickAsync();

        // Wait for page to reload
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert: share moved to "Resources Shared with Me" section
        await Expect(Page.GetByText("Gedeelde bronnen met mij")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Accept Test Account")).ToBeVisibleAsync();

        // Verify it is no longer in Pending section (no Accept button)
        var pendingSection = Page.Locator("section").First;
        await Expect(pendingSection.GetByText("Accept Test Account")).ToBeHiddenAsync(
            new LocatorAssertionsToBeHiddenOptions { Timeout = 5000 });
    }

    [Test]
    public async Task NavMenu_ShowsPendingBadgeCount()
    {
        // Arrange: create 2 pending shares for the seed user
        using (var scope = Factory!.CreateDbScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            for (int i = 0; i < 2; i++)
            {
                var account = new Account
                {
                    Id = Guid.NewGuid(),
                    Label = $"Badge Test Account {i}",
                    Type = AccountType.Checking,
                    IBAN = "NL91ABNA0417164300",
                    Currency = "EUR",
                    StartingBalance = 100,
                    UserId = SecondUserId
                };
                context.Accounts.Add(account);
                await context.SaveChangesAsync();

                context.AccountShares.Add(new AccountShare
                {
                    Id = Guid.NewGuid(),
                    AccountId = account.Id,
                    SharedWithUserId = AppDbContext.SeedUserId,
                    Permission = PermissionLevel.Viewer,
                    Status = ShareStatus.Pending,
                    UserId = SecondUserId
                });
            }
            await context.SaveChangesAsync();
        }

        // Act: navigate to home/accounts page so nav menu loads
        await Page.GotoAsync($"{BaseUrl}/sharing/shared-with-me", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        // Assert: NavMenu badge for "Shared with Me" shows "2"
        var badge = Page.GetByTestId("nav-pending-share-badge");
        await Expect(badge).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15000 });
        await Expect(badge).ToHaveTextAsync("2", new LocatorAssertionsToHaveTextOptions { Timeout = 15000 });
    }
}
