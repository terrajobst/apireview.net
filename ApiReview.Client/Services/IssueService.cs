using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

using ApiReview.Shared;

namespace ApiReview.Client.Services
{
    public sealed class IssueService
    {
        private readonly BackendHttpClientFactory _clientFactory;
        private IReadOnlyList<ApiReviewIssue> _issues;

        public IssueService(BackendHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task InvalidateAsync()
        {
            var client = await _clientFactory.CreateAsync();
            _issues = await client.GetFromJsonAsync<IReadOnlyList<ApiReviewIssue>>("issues", _clientFactory.JsonOptions);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public async Task<IReadOnlyList<ApiReviewIssue>> GetAsync()
        {
            if (_issues == null)
                await InvalidateAsync();

            return _issues;
        }

        public event EventHandler Changed;
    }
}
