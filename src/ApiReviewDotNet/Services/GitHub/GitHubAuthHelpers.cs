using ApiReviewDotNet.Data;

using Octokit.GraphQL;

namespace ApiReviewDotNet.Services.GitHub;

public static class GitHubAuthHelpers
{
    public static async Task<bool> IsMemberOfAnyTeamAsync(string accessToken, string orgName, IEnumerable<string> teamSlugs, string userName)
    {
        foreach (var teamSlug in teamSlugs)
        {
            if (await IsMemberOfTeamAsync(accessToken, orgName, teamSlug, userName))
                return true;
        }

        return false;
    }

    public static async Task<bool> IsMemberOfTeamAsync(string accessToken, string orgName, string teamSlug, string userName)
    {
        try
        {
            var productInformation = new ProductHeaderValue(ApiReviewConstants.ProductName);
            var connection = new Connection(productInformation, accessToken);

            var query = new Query()
                .Organization(orgName)
                .Team(teamSlug)
                .Members(null, null, null, null, null, null, userName, null)
                .AllPages()
                .Select(u => u.Login);

            var result = await connection.Run(query);
            var exactUser = result.FirstOrDefault(login => string.Equals(login, userName, StringComparison.OrdinalIgnoreCase));
            return exactUser is not null;
        }
        catch
        {
            return false;
        }
    }
}
