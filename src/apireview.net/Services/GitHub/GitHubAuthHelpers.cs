using ApiReviewDotNet.Data;
using Octokit;

namespace ApiReviewDotNet.Services.GitHub;

public static class GitHubAuthHelpers
{
    public static async Task<bool> IsMemberOfAnyTeamAsync(string accessToken, string orgName, IEnumerable<string> teamSlugs, string userName)
    {
        try
        {
            var productInformation = new ProductHeaderValue(ApiReviewConstants.ProductName);
            var client = new GitHubClient(productInformation)
            {
                Credentials = new Credentials(accessToken)
            };

            var teams = await client.Organization.Team.GetAll(orgName);
            var teamBySlug = teams.ToDictionary(t => t.Slug, StringComparer.OrdinalIgnoreCase);


            foreach (var teamSlug in teamSlugs)
            {
                if (!teamBySlug.TryGetValue(teamSlug, out var team))
                    continue;

                var membership = await client.Organization.Team.GetMembershipDetails(team.Id, userName);
                if (membership is not null && membership.State == MembershipState.Active)
                    return true;
            }
        }
        catch
        {
            // Ignore
        }

        return false;
    }
}
