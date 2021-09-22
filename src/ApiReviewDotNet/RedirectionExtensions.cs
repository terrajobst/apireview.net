using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Net.Http.Headers;

namespace ApiReviewDotNet
{
    public static class RedirectionExtensions
    {
        public static IApplicationBuilder UseHostRedirection(this IApplicationBuilder app, string oldHost, string newHost)
        {
            return app.Use(async (context, next) =>
            {
                var url = context.Request.GetUri();
                if (url.Host.Equals(oldHost, StringComparison.OrdinalIgnoreCase))
                {
                    var response = context.Response;
                    response.StatusCode = StatusCodes.Status301MovedPermanently;

                    var newUrl = new UriBuilder(url)
                    {
                        Host = newHost
                    }.ToString();

                    response.Headers[HeaderNames.Location] = newUrl;
                    return;
                }

                await next();
            });
        }
    }
}
