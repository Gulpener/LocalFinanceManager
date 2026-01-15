using LocalFinanceManager.Services.Background;

namespace LocalFinanceManager.Tests.Unit;

/// <summary>
/// Unit tests for CronParser utility.
/// </summary>
public class CronParserTests
{
    [Fact]
    public void GetNextOccurrence_Daily6AM_ReturnsCorrectTime()
    {
        // Arrange
        var cronExpression = "0 6 * * *"; // Daily at 6 AM
        var fromTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var nextRun = CronParser.GetNextOccurrence(cronExpression, fromTime);

        // Assert
        Assert.Equal(2026, nextRun.Year);
        Assert.Equal(1, nextRun.Month);
        Assert.Equal(16, nextRun.Day); // Next day
        Assert.Equal(6, nextRun.Hour);
        Assert.Equal(0, nextRun.Minute);
    }

    [Fact]
    public void GetNextOccurrence_WeeklySunday2AM_ReturnsCorrectTime()
    {
        // Arrange
        var cronExpression = "0 2 * * 0"; // Sunday 2 AM (0 = Sunday)
        var fromTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc); // Thursday

        // Act
        var nextRun = CronParser.GetNextOccurrence(cronExpression, fromTime);

        // Assert
        Assert.Equal(DayOfWeek.Sunday, nextRun.DayOfWeek);
        Assert.Equal(2, nextRun.Hour);
        Assert.Equal(0, nextRun.Minute);
        Assert.True(nextRun > fromTime); // Must be in the future
    }

    [Fact]
    public void GetNextOccurrence_EveryHour_ReturnsNextHour()
    {
        // Arrange
        var cronExpression = "0 * * * *"; // Every hour
        var fromTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var nextRun = CronParser.GetNextOccurrence(cronExpression, fromTime);

        // Assert
        Assert.Equal(11, nextRun.Hour); // Next hour
        Assert.Equal(0, nextRun.Minute);
    }

    [Fact]
    public void GetNextOccurrence_SpecificMinute_ReturnsCorrectTime()
    {
        // Arrange
        var cronExpression = "30 14 * * *"; // 2:30 PM daily
        var fromTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var nextRun = CronParser.GetNextOccurrence(cronExpression, fromTime);

        // Assert
        Assert.Equal(15, nextRun.Day); // Same day
        Assert.Equal(14, nextRun.Hour);
        Assert.Equal(30, nextRun.Minute);
    }

    [Fact]
    public void GetNextOccurrence_InvalidFormat_ThrowsException()
    {
        // Arrange
        var invalidCronExpression = "0 6 * *"; // Missing day-of-week field
        var fromTime = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CronParser.GetNextOccurrence(invalidCronExpression, fromTime));
    }

    [Fact]
    public void GetNextOccurrence_AfterScheduledTime_ReturnsNextDay()
    {
        // Arrange
        var cronExpression = "0 6 * * *"; // Daily at 6 AM
        var fromTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc); // After 6 AM

        // Act
        var nextRun = CronParser.GetNextOccurrence(cronExpression, fromTime);

        // Assert
        Assert.Equal(16, nextRun.Day); // Next day
        Assert.Equal(6, nextRun.Hour);
    }
}
