using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ApiReview.Shared;

namespace ApiReview.Client.Data
{
    internal sealed class ApiReviewNoteService
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiReviewNoteService(IOptions<JsonOptions> options)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:44305")
            };
            _jsonOptions = options.Value.JsonSerializerOptions;
        }

        public Task<ApiReviewSummary> IssuesForRange(DateTimeOffset start, DateTimeOffset end)
        {
            var url = $"notes/issues-for-range?start={start:s}&end={end:s}";
            return _client.GetFromJsonAsync<ApiReviewSummary>(url, _jsonOptions);
        }

        public Task<ApiReviewSummary> IssuesForVideo(string videoId)
        {
            var url = $"notes/issues-for-video?videoId={videoId}";
            return _client.GetFromJsonAsync<ApiReviewSummary>(url, _jsonOptions);
        }

        public Task<IReadOnlyList<ApiReviewVideo>> GetVideos(DateTimeOffset start, DateTimeOffset end)
        {
            var url = $"notes/videos?start={start:s}&end={end:s}";
            return _client.GetFromJsonAsync<IReadOnlyList<ApiReviewVideo>>(url, _jsonOptions);
        }
    }
}
