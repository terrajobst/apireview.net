using ApiReviewDotNet.Data;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Models;
using Octokit.Webhooks.Models.PullRequestEvent;

namespace ApiReviewDotNet.Services.GitHub;

public sealed class GitHubEventProcessor : WebhookEventProcessor
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

    private readonly ILogger<GitHubEventProcessor> _logger;
    private readonly IssueService _issueService;

    public GitHubEventProcessor(ILogger<GitHubEventProcessor> logger, IssueService issueService)
    {
        _logger = logger;
        _issueService = issueService;
    }

    public override async Task ProcessWebhookAsync(WebhookHeaders headers, WebhookEvent webhookEvent)
    {
        _logger.LogInformation("Received event {action} ({type})", webhookEvent.Action, webhookEvent.GetType().Name);

        if (IsRelevant(webhookEvent))
        {
            _logger.LogInformation("Message relevant, reloading issues");
            await _issueService.ReloadAsync();
        }
    }

    private static bool IsRelevant(WebhookEvent message)
    {
        if (message.Action is null || !_relevantActions.Contains(message.Action))
            return false;

        return message is LabelEvent labelEvent && IsRelevant(labelEvent.Label) ||
               message is IssuesEvent issuesEvent && IsRelevant(issuesEvent.Issue) ||
               message is PullRequestEvent pullRequestEvent && IsRelevant(pullRequestEvent.PullRequest);
    }

    private static bool IsRelevant(Issue? issue)
    {
        return issue is not null && IsRelevant(issue.Labels);
    }

    private static bool IsRelevant(PullRequest? issue)
    {
        return issue is not null &&
               IsRelevant(issue.Labels);
    }

    private static bool IsRelevant(IEnumerable<Label>? labels)
    {
        return labels is not null &&
               labels.Any(IsRelevant);
    }

    private static bool IsRelevant(Label? payload)
    {
        return payload is not null &&
               _relevantLabels.Contains(payload.Name);
    }
}
