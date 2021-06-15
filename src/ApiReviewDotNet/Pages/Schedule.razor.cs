using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace ApiReviewDotNet.Pages
{
    public sealed partial class Schedule
    {
        private static readonly string Url = "https://outlook.office365.com/owa/calendar/3b9be8f4136f47bfb3bd10638b946523@microsoft.com/dedb09caf32a4c23be118e9f97ad25717678617836187798260/calendar.ics";

        private CalendarCell[][] Rows { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var calendar = await LoadCalendarAsync(Url);
            Rows = GetRows(calendar, DateTime.Today).ToArray();
        }

        private static async Task<CalendarCollection> LoadCalendarAsync(string url)
        {
            var httpClient = new HttpClient();
            var content = await httpClient.GetStringAsync(url);
            return CalendarCollection.Load(content);
        }     

        private static IEnumerable<CalendarCell> GetCells(CalendarCollection calendars, DateTimeOffset date)
        {
            var firstDayInMonth = new DateTime(date.Year, date.Month, 1);
            var firstDay = firstDayInMonth.AddDays(-(int)firstDayInMonth.DayOfWeek);
            var lastDayInMonth = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
            var lastDay = lastDayInMonth.AddDays(6 - (int)firstDayInMonth.DayOfWeek - 1);

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
            var calStart = new CalDateTime(start.DateTime);
            var calEnd = new CalDateTime(end.DateTime);

            foreach (var occurence in calendars.GetOccurrences(calStart, calEnd))
            {
                var e = occurence.Source as CalendarEvent;
                if (e is not null)
                {
                    yield return new CalendarEntry
                    {
                        Title = e.Summary,
                        Description = e.Description,
                        Start = occurence.Period.StartTime.AsDateTimeOffset,
                        End = occurence.Period.EndTime.AsDateTimeOffset,
                    };
                }
            }
        }

        private static IEnumerable<CalendarCell[]> GetRows(CalendarCollection calendars, DateTime date)
        {
            var cells = GetCells(calendars, date);
            return GetRows(cells);
        }

        private static IEnumerable<CalendarCell[]> GetRows(IEnumerable<CalendarCell> cells)
        {
            var row = new List<CalendarCell>(7);

            foreach (var cell in cells)
            {
                row.Add(cell);
                if (row.Count == 7)
                {
                    yield return row.ToArray();
                    row.Clear();
                }
            }
        }

        private sealed class CalendarCell
        {
            public CalendarCell(DateTimeOffset dateTime, IEnumerable<CalendarEntry> entries)
            {
                DateTime = dateTime;
                Entries = entries.ToArray();
            }

            public DateTimeOffset DateTime { get; }
            public IReadOnlyList<CalendarEntry> Entries { get; }
        }

        private sealed class CalendarEntry
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTimeOffset Start { get; set; }
            public DateTimeOffset End { get; set; }
        }
    }
}
