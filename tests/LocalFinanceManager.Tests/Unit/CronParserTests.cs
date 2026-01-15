using LocalFinanceManager.Services.Background;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Unit;

/// <summary>
/// Unit tests for CronParser utility.
/// </summary>
[TestFixture]
public class CronParserTests
{
    [Test]
    public void GetNextOccurrence_Daily6AM_ReturnsCorrectTime()
    {
        // Arrange
        var cronExpression = "0 6 * * *"; // Daily at 6 AM
        var fromTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var nextRun = CronParser.GetNextOccurrence(cronExpression, fromTime);

        // Assert
        Assert.That(nextRun.Year, Is.EqualTo(2026));
        Assert.That(nextRun.Month, Is.EqualTo(1));
        Assert.That(nextRun.Day, Is.EqualTo(16)); // Next day
        Assert.That(nextRun.Hour, Is.EqualTo(6));
        Assert.That(nextRun.Minute, Is.EqualTo(0));
    }

    [Test]
    public void GetNextOccurrence_WeeklySunday2AM_ReturnsCorrectTime()
    {
        // Arrange
        var cronExpression = "0 2 * * 0"; // Sunday 2 AM (0 = Sunday)
        var fromTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc); // Thursday

        // Act
        var nextRun = CronParser.GetNextOccurrence(cronExpression, fromTime);

        // Assert
        Assert.That(nextRun.DayOfWeek, Is.EqualTo(DayOfWeek.Sunday));
        Assert.That(nextRun.Hour, Is.EqualTo(2));
        Assert.That(nextRun.Minute, Is.EqualTo(0));
        Assert.That(nextRun > fromTime, Is.True); // Must be in the future
    }

    [Test]
    public void GetNextOccurrence_EveryHour_ReturnsNextHour()
    {
        // Arrange
        var cronExpression = "0 * * * *"; // Every hour
        var fromTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var nextRun = CronParser.GetNextOccurrence(cronExpression, fromTime);

        // Assert
        Assert.That(nextRun.Hour, Is.EqualTo(11)); // Next hour
        Assert.That(nextRun.Minute, Is.EqualTo(0));
    }

    [Test]
    public void GetNextOccurrence_SpecificMinute_ReturnsCorrectTime()
    {
        // Arrange
        var cronExpression = "30 14 * * *"; // 2:30 PM daily
        var fromTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var nextRun = CronParser.GetNextOccurrence(cronExpression, fromTime);

        // Assert
        Assert.That(nextRun.Day, Is.EqualTo(15)); // Same day
        Assert.That(nextRun.Hour, Is.EqualTo(14));
        Assert.That(nextRun.Minute, Is.EqualTo(30));
    }

    [Test]
    public void GetNextOccurrence_InvalidFormat_ThrowsException()
    {
        // Arrange
        var invalidCronExpression = "0 6 * *"; // Missing day-of-week field
        var fromTime = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CronParser.GetNextOccurrence(invalidCronExpression, fromTime));
    }

    [Test]
    public void GetNextOccurrence_AfterScheduledTime_ReturnsNextDay()
    {
        // Arrange
        var cronExpression = "0 6 * * *"; // Daily at 6 AM
        var fromTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc); // After 6 AM

        // Act
        var nextRun = CronParser.GetNextOccurrence(cronExpression, fromTime);

        // Assert
        Assert.That(nextRun.Day, Is.EqualTo(16)); // Next day
        Assert.That(nextRun.Hour, Is.EqualTo(6));
    }
}
