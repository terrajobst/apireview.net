
using Microsoft.AspNetCore.Authentication;

namespace ApiReview.Server.GitHubAuth
{
    internal static class GitHubBearerExtensions
    {
        public static AuthenticationBuilder AddGitHubBearer(this AuthenticationBuilder builder)
        {
            string authenticationScheme = "Bearer";
            string displayName = "GitHub";
            return builder.AddScheme<AuthenticationSchemeOptions, GitHubBearerHandler>(authenticationScheme, displayName, null);
        }
    }
}
