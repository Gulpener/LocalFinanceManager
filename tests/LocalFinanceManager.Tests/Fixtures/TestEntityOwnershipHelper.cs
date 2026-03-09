using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LocalFinanceManager.Tests.Fixtures;

/// <summary>
/// Shared helper for test database contexts that automatically assigns the default test user ID
/// to newly-added entities that have a nullable <c>UserId</c> property.
/// <para>
/// Subscribe the two EF Core ChangeTracker events to <see cref="Apply"/> in your test setup:
/// <code>
/// _context.ChangeTracker.Tracked     += (_, args) => TestEntityOwnershipHelper.Apply(args.Entry);
/// _context.ChangeTracker.StateChanged += (_, args) => TestEntityOwnershipHelper.Apply(args.Entry);
/// </code>
/// </para>
/// </summary>
public static class TestEntityOwnershipHelper
{
    /// <summary>
    /// Assigns <see cref="TestUserContext.DefaultUserId"/> to the entity's <c>UserId</c> property
    /// when the entity has just been marked as <see cref="EntityState.Added"/> and the property is null.
    /// </summary>
    public static void Apply(EntityEntry entry)
    {
        if (entry.State != EntityState.Added)
        {
            return;
        }

        var userId = TestUserContext.DefaultUserId;

        switch (entry.Entity)
        {
            case Account entity when entity.UserId == null:
                entity.UserId = userId;
                break;
            case BudgetPlan entity when entity.UserId == null:
                entity.UserId = userId;
                break;
            case Category entity when entity.UserId == null:
                entity.UserId = userId;
                break;
            case BudgetLine entity when entity.UserId == null:
                entity.UserId = userId;
                break;
            case Transaction entity when entity.UserId == null:
                entity.UserId = userId;
                break;
            case AppSettings entity when entity.UserId == null:
                entity.UserId = userId;
                break;
        }
    }
}
