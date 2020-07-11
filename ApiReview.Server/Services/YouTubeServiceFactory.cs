using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

using Microsoft.Extensions.Configuration;

namespace ApiReview.Server.Services
{
    public sealed class YouTubeServiceFactory
    {
        private readonly IConfiguration _configuration;

        public YouTubeServiceFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<YouTubeService> CreateAsync()
        {
            var secrets = new ClientSecrets()
            {
                ClientId = _configuration["YouTubeClientId"],
                ClientSecret = _configuration["YouTubeClientSecret"]
            };

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                // This OAuth 2.0 access scope allows for full read/write access to the
                // authenticated user's account.
                new[] {
                    YouTubeService.Scope.Youtube,
                    YouTubeService.Scope.YoutubeForceSsl
                },
                "user",
                CancellationToken.None
            );

            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            };

            return new YouTubeService(initializer);
        }
    }
}
