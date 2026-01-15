using System.Globalization;

namespace LocalFinanceManager.Services.Background;

/// <summary>
/// Simple cron expression parser for scheduling background jobs.
/// Supports basic 5-field cron format: minute hour day-of-month month day-of-week
/// </summary>
public static class CronParser
{
    /// <summary>
    /// Calculates the next occurrence time for a cron expression from the given start time.
    /// </summary>
    /// <param name="cronExpression">Cron expression in "minute hour day-of-month month day-of-week" format</param>
    /// <param name="fromTime">Start time to calculate from (UTC)</param>
    /// <returns>Next occurrence time in UTC</returns>
    public static DateTime GetNextOccurrence(string cronExpression, DateTime fromTime)
    {
        var parts = cronExpression.Split(' ');
        if (parts.Length != 5)
        {
            throw new ArgumentException("Cron expression must have 5 fields: minute hour day-of-month month day-of-week");
        }

        var minute = ParseField(parts[0], 0, 59);
        var hour = ParseField(parts[1], 0, 23);
        var dayOfMonth = ParseField(parts[2], 1, 31);
        var month = ParseField(parts[3], 1, 12);
        var dayOfWeek = ParseField(parts[4], 0, 6); // 0 = Sunday

        // Start from the next minute
        var candidate = fromTime.AddMinutes(1);
        candidate = new DateTime(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, candidate.Minute, 0, DateTimeKind.Utc);

        // Search for next match (max 4 years ahead to avoid infinite loop)
        var maxIterations = 366 * 4 * 24 * 60; // 4 years in minutes
        for (int i = 0; i < maxIterations; i++)
        {
            if (Matches(candidate, minute, hour, dayOfMonth, month, dayOfWeek))
            {
                return candidate;
            }
            candidate = candidate.AddMinutes(1);
        }

        throw new InvalidOperationException($"Could not find next occurrence for cron expression: {cronExpression}");
    }

    private static List<int> ParseField(string field, int min, int max)
    {
        if (field == "*")
        {
            return Enumerable.Range(min, max - min + 1).ToList();
        }

        if (field.Contains(','))
        {
            return field.Split(',').Select(int.Parse).ToList();
        }

        if (field.Contains('/'))
        {
            var parts = field.Split('/');
            var step = int.Parse(parts[1]);
            var start = parts[0] == "*" ? min : int.Parse(parts[0]);
            var result = new List<int>();
            for (int i = start; i <= max; i += step)
            {
                result.Add(i);
            }
            return result;
        }

        if (field.Contains('-'))
        {
            var parts = field.Split('-');
            var start = int.Parse(parts[0]);
            var end = int.Parse(parts[1]);
            return Enumerable.Range(start, end - start + 1).ToList();
        }

        return new List<int> { int.Parse(field) };
    }

    private static bool Matches(DateTime time, List<int> minutes, List<int> hours, List<int> daysOfMonth, List<int> months, List<int> daysOfWeek)
    {
        if (!minutes.Contains(time.Minute)) return false;
        if (!hours.Contains(time.Hour)) return false;
        if (!months.Contains(time.Month)) return false;

        // For day matching, cron uses OR logic: match if either day-of-month OR day-of-week matches
        var dayOfWeekMatches = daysOfWeek.Contains((int)time.DayOfWeek);
        var dayOfMonthMatches = daysOfMonth.Contains(time.Day);

        // If both fields are unrestricted (*), match
        if (daysOfMonth.Count == 31 && daysOfWeek.Count == 7)
        {
            return true;
        }

        // If only day-of-month is restricted
        if (daysOfMonth.Count < 31 && daysOfWeek.Count == 7)
        {
            return dayOfMonthMatches;
        }

        // If only day-of-week is restricted
        if (daysOfWeek.Count < 7 && daysOfMonth.Count == 31)
        {
            return dayOfWeekMatches;
        }

        // If both are restricted, use OR logic
        return dayOfMonthMatches || dayOfWeekMatches;
    }
}
