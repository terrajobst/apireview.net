using System.Collections.Generic;

namespace ApiReview.Shared
{
    public static class ApiReviewConstants
    {
        public const string ProductName = "apireview.azurewebsites.net";
        public const string ApiApproverOrgName = "dotnet";
        public const string ApiApproverTeamSlug = "fxdc";
        public const string ApiApproverRole = "api-approver";
        public const string GitHubScopesClaim = "github-scopes";
        public static readonly IReadOnlyList<string> GitHubScopes = new[] { "read:org", "repo" };
        public static readonly string GitHubScopeString = string.Join(",", GitHubScopes);
        public const string GitHubAvatarUrl = "avatar_url";
        public const string RepoList = "dotnet/designs,dotnet/runtime,dotnet/winforms";
        public const string NetFoundationChannelId = "UCiaZbznpWV1o-KLxj8zqR6A";
        public const string ApiReviewsOrgName = "dotnet";
        public const string ApiReviewsRepoName = "apireviews";
        public const string ApiReviewsBranch = "master";
        public const string ApiReadyForReview = "api-ready-for-review";
        public const string ApiNeedsWork = "api-needs-work";
        public const string ApiApproved = "api-approved";
    }
}
