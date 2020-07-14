using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using ApiReview.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

using Octokit.GraphQL;

namespace ApiReview.Server.GitHubAuth
{
    internal sealed class GitHubBearerHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public GitHubBearerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                                   ILoggerFactory logger,
                                   UrlEncoder encoder,
                                   ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authorization = (string)Request.Headers[HeaderNames.Authorization];

            // If no authorization header found, nothing to process further
            if (string.IsNullOrEmpty(authorization))
                return AuthenticateResult.NoResult();

            var token = string.Empty;
            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = authorization.Substring("Bearer ".Length).Trim();

            // If no token found, no further work possible
            if (string.IsNullOrEmpty(token))
                return AuthenticateResult.NoResult();

            // Now use the GitHub APIs to validate the token & check whether the sender
            // is a member of our team.

            string userName;
            bool isApiApprover;

            try
            {
                (userName, isApiApprover) = await GetUserInfo(token);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex);
            }

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userName)
            }, "Bearer");

            if (isApiApprover)
                identity.AddClaim(new Claim(identity.RoleClaimType, ApiReviewConstants.ApiApproverRole));
            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(principal, "Bearer");
            var authenticationToken = new AuthenticationToken
            {
                Name = "access_token",
                Value = token
            };
            ticket.Properties.StoreTokens(new[] { authenticationToken });

            return AuthenticateResult.Success(ticket);
        }

        private async Task<(string UserName, bool IsApiApprover)> GetUserInfo(string token)
        {
            var productInformation = new ProductHeaderValue(ApiReviewConstants.ProductName);
            var connection = new Connection(productInformation, token);

            var query = new Query()
                .Select(q => new
                {
                    User = q.Viewer.Select(u => u.Login).Single(),
                    TeamMembers = q.Organization(ApiReviewConstants.ApiApproverOrgName)
                                   .Team(ApiReviewConstants.ApiApproverTeamSlug)
                                   .Members(null, null, null, null, null, null, null, null)
                                   .AllPages()
                                   .Select(u => u.Login)
                                   .ToList()
                });

            var result = await connection.Run(query);
            var user = result.User;
            var userIsMember = result.TeamMembers.Find(login => string.Equals(login, user, StringComparison.OrdinalIgnoreCase)) != null;
            return (user, userIsMember);
        }
    }
}
