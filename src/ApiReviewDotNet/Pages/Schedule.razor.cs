using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using ApiReviewDotNet.Data;

using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

using Microsoft.AspNetCore.Components;

namespace ApiReviewDotNet.Pages
{
    public sealed partial class Schedule
    {
        private static readonly string Url = "https://outlook.office365.com/owa/calendar/3b9be8f4136f47bfb3bd10638b946523@microsoft.com/dedb09caf32a4c23be118e9f97ad25717678617836187798260/calendar.ics";

        private DateTimeOffset? Today { get; set; }
        private DateTimeOffset? CurrentDate { get; set; }

        private CalendarCell[] Cells { get; set; }

        [Inject]
        public TimeZoneService TimeZoneService { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await UpdateCellsAsync();
        }

        private async Task UpdateCellsAsync()
        {
            if (Today is null)
            {
                var userDateTime = await TimeZoneService.ToLocalAsync(DateTime.UtcNow);
                Today = new DateTimeOffset(userDateTime.Year, userDateTime.Month, userDateTime.Day, 0, 0, 0, 0, userDateTime.Offset);
            }

            if (CurrentDate is null)
                CurrentDate = Today;

            var calendar = await LoadCalendarAsync(Url);
            Cells = GetCells(calendar, CurrentDate.Value).ToArray();
            StateHasChanged();
        }

        private static async Task<CalendarCollection> LoadCalendarAsync(string url)
        {
            var httpClient = new HttpClient();
            var content = await httpClient.GetStringAsync(url);
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
            var calStart = new CalDateTime(start.UtcDateTime);
            var calEnd = new CalDateTime(end.UtcDateTime);

            foreach (var occurence in calendars.GetOccurrences(calStart, calEnd))
            {
                var e = occurence.Source as CalendarEvent;
                if (e is not null)
                {
                    yield return new CalendarEntry
                    {
                        Title = e.Summary,
                        Description = e.Description,
                        Start = occurence.Period.StartTime.AsDateTimeOffset.ToOffset(start.Offset),
                        End = occurence.Period.EndTime.AsDateTimeOffset.ToOffset(start.Offset),
                    };
                }
            }
        }

        private async Task TodayAsync()
        {
            CurrentDate = null;
            await UpdateCellsAsync();
        }

        private async Task PreviousMonthAsync()
        {
            CurrentDate = CurrentDate.Value.AddMonths(-1);
            await UpdateCellsAsync();
        }

        private async Task NextMonthAsync()
        {
            CurrentDate = CurrentDate.Value.AddMonths(1);
            await UpdateCellsAsync();
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
