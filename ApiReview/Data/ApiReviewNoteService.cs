using System;
using System.Collections.Generic;
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

        public Task<ApiReviewSummary> GetByDate(DateTimeOffset date)
        {
            var url = "notes/date/" + date.ToString("s");
            return _client.GetFromJsonAsync<ApiReviewSummary>(url);
        }

        public Task<ApiReviewSummary> GetByVideo(string videoId)
        {
            var url = "notes/video/" + videoId;
            return _client.GetFromJsonAsync<ApiReviewSummary>(url);
        }

        public Task<IReadOnlyList<ApiReviewVideo>> GetVideos(DateTimeOffset date)
        {
            var url = "notes/videos/" + date.ToString("s");
            return _client.GetFromJsonAsync<IReadOnlyList<ApiReviewVideo>>(url);
        }
    }
}
