namespace ApiReviewDotNet.Services.Calendar;

public sealed class CalendarEntry
{
    public CalendarEntry(string title,
                         string description,
                         DateTimeOffset start,
                         DateTimeOffset end)
    {
        Title = title;
        Description = description;
        Start = start;
        End = end;
    }

    public string Title { get; }
    public string Description { get; }
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
}
