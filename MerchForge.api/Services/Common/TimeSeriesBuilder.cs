using MerchForge.api.DTOs.Common;

namespace MerchForge.api.Services.Common
{
    public static class TimeSeriesBuilder
    {
        public static List<TimeSeriesPointResponse> BuildMonthlySeries(
            List<DateTime> dates,
            DateTime seriesStart,
            DateTime until)
        {
            var series = new List<TimeSeriesPointResponse>();

            var cursor = seriesStart;
            var end = new DateTime(until.Year, until.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            while (cursor <= end)
            {
                var nextMonth = cursor.AddMonths(1);

                var count = dates.Count(d => d >= cursor && d < nextMonth);

                series.Add(new TimeSeriesPointResponse
                {
                    Period = cursor.ToString("yyyy-MM"),
                    Count = count,
                });

                cursor = nextMonth;
            }

            return series;
        }

        public static List<TimeSeriesPointResponse> BuildDailySeries(
            List<DateTime> dates,
            DateTime seriesStart,
            DateTime until)
        {
            var series = new List<TimeSeriesPointResponse>();

            var cursor = seriesStart.Date;
            var end = until.Date;

            while (cursor <= end)
            {
                var nextDay = cursor.AddDays(1);

                var count = dates.Count(d => d >= cursor && d < nextDay);

                series.Add(new TimeSeriesPointResponse
                {
                    Period = cursor.ToString("yyyy-MM-dd"),
                    Count = count,
                });

                cursor = nextDay;
            }

            return series;
        }
    }
}
