namespace ApiReviewDotNet.Services.Calendar;

public sealed class CalendarCell
{
    public CalendarCell(DateTimeOffset dateTime, IEnumerable<CalendarEntry> entries)
    {
        DateTime = dateTime;
        Entries = entries.ToArray();
    }

    public DateTimeOffset DateTime { get; }
    public IReadOnlyList<CalendarEntry> Entries { get; }
}
