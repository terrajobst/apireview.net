using ApiReviewDotNet.Data;

using Terrajobst.GitHubEvents;

namespace ApiReviewDotNet.Services.GitHub;

public sealed class GitHubEvenProcessor : IGitHubEventProcessor
{
    private static readonly HashSet<string> _relevantActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "opened",
        "edited",
        "deleted",
        "closed",
        "reopened",
        "assigned",
        "unassigned",
        "labeled",
        "unlabeled",
        "transferred",
        "milestoned",
        "demilestoned"
    };

    private static readonly HashSet<string> _relevantLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ApiReviewConstants.ApiReadyForReview,
        ApiReviewConstants.ApiApproved,
        ApiReviewConstants.ApiNeedsWork
    };

    private readonly IssueService _issueService;

    public GitHubEvenProcessor(IssueService issueService)
    {
        _issueService = issueService;
    }

    public async void Process(GitHubEventMessage message)
    {
        if (IsRelevant(message))
            await _issueService.ReloadAsync();
    }

    private static bool IsRelevant(GitHubEventMessage message)
    {
        if (message.Body.Action is null || !_relevantActions.Contains(message.Body.Action))
            return false;

        return IsRelevant(message.Body.Label) ||
               IsRelevant(message.Body.Issue) ||
               IsRelevant(message.Body.PullRequest);
    }

    private static bool IsRelevant(GitHubEventIssueOrPullRequest? issueOrPullRequest)
    {
        if (issueOrPullRequest is null || issueOrPullRequest.Labels is null)
            return false;

        return IsRelevant(issueOrPullRequest.Labels);
    }

    private static bool IsRelevant(IEnumerable<GitHubEventLabel>? labels)
    {
        if (labels is null)
            return false;

        return labels.Any(IsRelevant);
    }

    private static bool IsRelevant(GitHubEventLabel? payload)
    {
        if (payload is null || payload.Name is null)
            return false;

        return _relevantLabels.Contains(payload.Name);
    }
}
