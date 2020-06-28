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

        public Task<ApiReviewSummary> IssuesForRange(DateTimeOffset start, DateTimeOffset end, bool video)
        {
            var url = $"notes/issues-for-range?start={start:s}&end={end:s}&video={video}";
            return _client.GetFromJsonAsync<ApiReviewSummary>(url);
        }

        public Task<ApiReviewSummary> IssuesForVideo(string videoId)
        {
            var url = $"notes/issues-for-video?videoId={videoId}";
            return _client.GetFromJsonAsync<ApiReviewSummary>(url);
        }

        public Task<IReadOnlyList<ApiReviewVideo>> GetVideos(DateTimeOffset start, DateTimeOffset end)
        {
            var url = $"notes/videos?start={start:s}&end={end:s}";
            return _client.GetFromJsonAsync<IReadOnlyList<ApiReviewVideo>>(url);
        }
    }
}
