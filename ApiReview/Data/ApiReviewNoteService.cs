using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ApiReview.Data
{
    internal sealed class ApiReviewNoteService
    {
        private readonly HttpClient _client;

        public ApiReviewNoteService()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:44305")
            };
        }

        public Task<ApiReviewSummary> GetSummaryAsync(DateTimeOffset date)
        {
            var url = "notes/" + date.ToString("s");
            return _client.GetFromJsonAsync<ApiReviewSummary>(url);
        }
    }
}
