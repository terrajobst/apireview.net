using System.Threading.Tasks;

using ApiReview.Shared;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

using Octokit;

namespace ApiReview.Server.Services
{
    public sealed class GitHubClientFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GitHubClientFactory(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<GitHubClient> CreateAsync()
        {
            var accessToken = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
            var productInformation = new ProductHeaderValue(ApiReviewConstants.ProductName);
            var client = new GitHubClient(productInformation)
            {
                Credentials = new Credentials(accessToken)
            };
            return client;
        }
    }
}
