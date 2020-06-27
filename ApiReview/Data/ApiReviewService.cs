using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ApiReview.Data
{
    internal sealed class ApiReviewService
    {
        private readonly HttpClient _client;
        private IReadOnlyList<ApiReviewIssue> _issues;

        public ApiReviewService()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:44305")
            };
        }

        public async Task<IReadOnlyList<ApiReviewIssue>> GetIssuesAsync()
        {
            if (_issues == null)
                await RefreshAsync();

            return _issues;
        }

        public async Task RefreshAsync()
        {
            _issues = await _client.GetFromJsonAsync<IReadOnlyList<ApiReviewIssue>>("issues");
        }
    }
}
