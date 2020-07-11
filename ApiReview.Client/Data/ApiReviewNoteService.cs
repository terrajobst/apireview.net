using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

using ApiReview.Shared;

namespace ApiReview.Client.Data
{
    internal sealed class ApiReviewNoteService
    {
        private readonly ApiReviewHttpClientFactory _clientFactory;

        public ApiReviewNoteService(ApiReviewHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<ApiReviewSummary> IssuesForRange(DateTimeOffset start, DateTimeOffset end)
        {
            var client = await _clientFactory.CreateAsync();
            var url = $"notes/issues-for-range?start={start:s}&end={end:s}";
            return await client.GetFromJsonAsync<ApiReviewSummary>(url, _clientFactory.JsonOptions);
        }

        public async Task<ApiReviewSummary> IssuesForVideo(string videoId)
        {
            var client = await _clientFactory.CreateAsync();
            var url = $"notes/issues-for-video?videoId={videoId}";
            return await client.GetFromJsonAsync<ApiReviewSummary>(url, _clientFactory.JsonOptions);
        }

        public async Task<IReadOnlyList<ApiReviewVideo>> GetVideos(DateTimeOffset start, DateTimeOffset end)
        {
            var client = await _clientFactory.CreateAsync();
            var url = $"notes/videos?start={start:s}&end={end:s}";
            return await client.GetFromJsonAsync<IReadOnlyList<ApiReviewVideo>>(url, _clientFactory.JsonOptions);
        }
    }
}
