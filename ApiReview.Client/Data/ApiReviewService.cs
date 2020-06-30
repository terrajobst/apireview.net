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
    internal sealed class ApiReviewService
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        private IReadOnlyList<ApiReviewIssue> _issues;

        public ApiReviewService(IOptions<JsonOptions> options)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:44305")
            };
            _jsonOptions = options.Value.JsonSerializerOptions;
        }

        public async Task<IReadOnlyList<ApiReviewIssue>> GetIssuesAsync()
        {
            if (_issues == null)
                await RefreshAsync();

            return _issues;
        }

        public async Task RefreshAsync()
        {
            _issues = await _client.GetFromJsonAsync<IReadOnlyList<ApiReviewIssue>>("issues", _jsonOptions);
        }
    }
}
