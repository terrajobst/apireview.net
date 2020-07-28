using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

using ApiReview.Shared;

namespace ApiReview.Client.Services
{
    internal sealed class NotesService
    {
        private readonly AuthenticatedBackendHttpClientFactory _clientFactory;

        public NotesService(AuthenticatedBackendHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IReadOnlyList<ApiReviewVideo>> GetVideos(DateTimeOffset start, DateTimeOffset end)
        {
            var client = await _clientFactory.CreateAsync();
            var url = $"notes/videos?start={start:s}&end={end:s}";
            return await client.GetFromJsonAsync<IReadOnlyList<ApiReviewVideo>>(url, _clientFactory.JsonOptions);
        }

        public async Task<ApiReviewVideo> GetVideo(string videoId)
        {
            var client = await _clientFactory.CreateAsync();
            var url = $"notes/video/{videoId}";
            return await client.GetFromJsonAsync<ApiReviewVideo>(url, _clientFactory.JsonOptions);
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

        public async Task<ApiReviewPublicationResult> PublishNotesAsync(ApiReviewSummary summary)
        {
            try
            {
                var client = await _clientFactory.CreateAsync();
                var url = $"notes/publish";
                var response = await client.PostAsJsonAsync(url, summary, _clientFactory.JsonOptions);
                return await response.Content.ReadFromJsonAsync<ApiReviewPublicationResult>(_clientFactory.JsonOptions);
            }
            catch
            {
                return ApiReviewPublicationResult.Failed();
            }
        }
    }
}
