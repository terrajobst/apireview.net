using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

using ApiReview.Shared;

namespace ApiReview.Client.Services
{
    internal sealed class IssueService
    {
        private readonly BackendHttpClientFactory _clientFactory;
        private IReadOnlyList<ApiReviewIssue> _issues;

        public IssueService(BackendHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IReadOnlyList<ApiReviewIssue>> GetAsync()
        {
            if (_issues == null)
                await RefreshAsync();

            return _issues;
        }

        public async Task RefreshAsync()
        {
            var client = await _clientFactory.CreateAsync();
            _issues = await client.GetFromJsonAsync<IReadOnlyList<ApiReviewIssue>>("issues", _clientFactory.JsonOptions);
        }
    }
}
