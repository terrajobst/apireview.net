using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace ApiReviewDotNet.Services.Calendar;

public sealed class CalendarService
{
    public string Url => "https://outlook.office365.com/owa/calendar/af2b46caf68d451787ff1b121ed50a40@microsoft.com/0a462958ba9e4b0785d278b96308193212993146288461861038/calendar.ics";

    private readonly IHttpClientFactory _httpClientFactory;

    public CalendarService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IEnumerable<CalendarCell>> GetCellsAsync(DateTimeOffset date)
    {
        var calendar = await LoadCalendarAsync(Url);
        return GetCells(calendar, date).ToArray();
    }

    private async Task<CalendarCollection> LoadCalendarAsync(string url)
    {
        using var httpClient = _httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "apireview.net");

        using var response = await httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        return CalendarCollection.Load(content);
    }

    private static IEnumerable<CalendarCell> GetCells(CalendarCollection calendars, DateTimeOffset date)
    {
        var firstDayInMonth = new DateTimeOffset(date.Year, date.Month, 1, 0, 0, 0, date.Offset);
        var firstDay = firstDayInMonth.AddDays(-(int)firstDayInMonth.DayOfWeek);
        var lastDayInMonth = new DateTimeOffset(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 0, 0, 0, date.Offset);
        var lastDay = lastDayInMonth.AddDays(6 - (int)lastDayInMonth.DayOfWeek);

        var current = firstDay;
        while (current <= lastDay)
        {
            var entries = GetEntries(calendars, current, current.AddDays(1));
            var cell = new CalendarCell(current, entries);
            yield return cell;
            current = current.AddDays(1);
        }
    }

    private static IEnumerable<CalendarEntry> GetEntries(CalendarCollection calendars, DateTimeOffset start, DateTimeOffset end)
    {
        IDateTime calStart = new CalDateTime(start.UtcDateTime);
        IDateTime calEnd = new CalDateTime(end.UtcDateTime);

        var timeZone = calendars.FirstOrDefault()?.TimeZones?.FirstOrDefault()?.TzId;
        if (timeZone is not null)
        {
            calStart = calStart.ToTimeZone(timeZone);
            calEnd = calEnd.ToTimeZone(timeZone);
        }

        foreach (var occurence in calendars.GetOccurrences(calStart, calEnd))
        {
            var e = occurence.Source as CalendarEvent;
            if (e is not null)
            {
                yield return new CalendarEntry(
                    title: e.Summary,
                    description: e.Description,
                    start: occurence.Period.StartTime.AsDateTimeOffset.ToOffset(start.Offset),
                    end: occurence.Period.EndTime.AsDateTimeOffset.ToOffset(start.Offset)
                );
            }
        }
    }
}
