
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

using Microsoft.Extensions.Configuration;

namespace ApiReviewDotNet.Services.YouTube
{
    public sealed class YouTubeServiceFactory
    {
        private readonly IConfiguration _configuration;

        public YouTubeServiceFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public YouTubeService Create()
        {
            var initializer = new BaseClientService.Initializer
            {
                ApiKey = _configuration["YouTubeKey"]
            };

            return new YouTubeService(initializer);
        }
    }
}
