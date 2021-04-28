using ApiReview.Shared;

using Microsoft.Extensions.Configuration;

using Octokit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiReview.Server.Services
{
    public interface IGitHubManager
    {
        Task<IReadOnlyList<ApiReviewFeedback>> GetFeedbackAsync(DateTimeOffset start, DateTimeOffset end);
        Task<IReadOnlyList<ApiReviewIssue>> GetIssuesAsync();
    }

    public sealed class FakeGitHubManager : IGitHubManager
    {
        private readonly IReadOnlyList<ApiReviewIssue> _issues;
        private readonly IReadOnlyList<ApiReviewFeedback> _feedback;

        public FakeGitHubManager()
        {
            _issues = JsonSerializer.Deserialize<IReadOnlyList<ApiReviewIssue>>(Resources.GitHubFakeIssues);
            _feedback = JsonSerializer.Deserialize<IReadOnlyList<ApiReviewFeedback>>(Resources.GitHubFakeFeedback);
        }

        public Task<IReadOnlyList<ApiReviewFeedback>> GetFeedbackAsync(DateTimeOffset start, DateTimeOffset end)
        {
            var result = _feedback.Where(f => start <= f.FeedbackDateTime && f.FeedbackDateTime <= end)
                                  .ToArray();

            return Task.FromResult<IReadOnlyList<ApiReviewFeedback>>(result);
        }

        public Task<IReadOnlyList<ApiReviewIssue>> GetIssuesAsync()
        {
            return Task.FromResult(_issues);
        }
    }

    public sealed class GitHubManager : IGitHubManager
    {
        private readonly IConfiguration _configuration;
        private readonly GitHubClientFactory _clientFactory;
        private readonly OspoService _ospoService;

        public GitHubManager(IConfiguration configuration, GitHubClientFactory clientFactory, OspoService ospoService)
        {
            _configuration = configuration;
            _clientFactory = clientFactory;
            _ospoService = ospoService;
        }

        public Task<IReadOnlyList<ApiReviewFeedback>> GetFeedbackAsync(DateTimeOffset start, DateTimeOffset end)
        {
            var repoList = _configuration["RepoList"];
            var repos = OrgAndRepo.ParseList(repoList).ToArray();
            return GetFeedbackAsync(repos, start, end);
        }

        private async Task<IReadOnlyList<ApiReviewFeedback>> GetFeedbackAsync(OrgAndRepo[] repos, DateTimeOffset start, DateTimeOffset end)
        {
            static bool MightBeAnApiIssue(Issue issue)
            {
                var isClosed = issue.State.Value == ItemState.Closed;
                var isReadyForReview = issue.Labels.Any(l => l.Name == ApiReviewConstants.ApiReadyForReview);
                var isApproved = issue.Labels.Any(l => l.Name == ApiReviewConstants.ApiApproved);
                var needsWork = issue.Labels.Any(l => l.Name == ApiReviewConstants.ApiNeedsWork);
                return isClosed || isReadyForReview || isApproved || needsWork;
            }

            static (string VideoLink, string Markdown) ParseFeedback(string body)
            {
                if (body == null)
                    return (null, null);

                const string prefix = "[Video](";
                if (body.StartsWith(prefix))
                {
                    var videoUrlEnd = body.IndexOf(")");
                    if (videoUrlEnd > 0)
                    {
                        var videoUrlStart = prefix.Length;
                        var videoUrlLength = videoUrlEnd - videoUrlStart;
                        var videoUrl = body.Substring(videoUrlStart, videoUrlLength);
                        var remainingBody = body.Substring(videoUrlEnd + 1).TrimStart();
                        return (videoUrl, remainingBody);
                    }
                }

                return (null, body);
            }

            // NOTE: Ideally, we'd use the user here, but if we do, we get a FORBIDDEN error.
            //       Not a biggie though, this API is only called by people with the api-approver
            //       role, so using the app quota seems fine.

            var github = await _clientFactory.CreateForAppAsync();
            var results = new List<ApiReviewFeedback>();

            foreach (var (owner, repo) in repos)
            {
                var request = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = ItemStateFilter.All,
                    Since = start
                };

                var issues = await github.Issue.GetAllForRepository(owner, repo, request);

                foreach (var issue in issues)
                {
                    if (!MightBeAnApiIssue(issue))
                        continue;

                    var events = await github.Issue.Events.GetAllForIssue(owner, repo, issue.Number);
                    var readyEvent = ApiReadyEvent.Get(events, issue.CreatedAt, end);
                    var reviewOutcome = ApiReviewOutcome.Get(events, start, end);

                    if (reviewOutcome != null)
                    {
                        var title = GitHubIssueHelpers.FixTitle(issue.Title);
                        var feedbackDateTime = reviewOutcome.DecisionTime;

                        var decision = reviewOutcome.Decision;
                        var comments = await github.Issue.Comment.GetAllForIssue(owner, repo, issue.Number);
                        var comment = comments.Where(c => start <= c.CreatedAt && c.CreatedAt <= end)
                                              .Where(c => string.Equals(c.User.Login, reviewOutcome.DecisionMaker, StringComparison.OrdinalIgnoreCase))
                                              .Select(c => (Comment: c, TimeDifference: Math.Abs((c.CreatedAt - feedbackDateTime).TotalSeconds)))
                                              .OrderBy(c => c.TimeDifference)
                                              .Select(c => c.Comment)
                                              .FirstOrDefault();

                        var feedbackId = comment?.Id.ToString();
                        var feedbackAuthor = reviewOutcome.DecisionMaker;
                        var feedbackUrl = comment?.HtmlUrl ?? issue.HtmlUrl;
                        var (videoUrl, feedbackMarkdown) = ParseFeedback(comment?.Body);

                        var apiReviewIssue = CreateIssue(owner, repo, issue);
                        apiReviewIssue.AreaOwner = readyEvent?.DecisionMaker;
                        apiReviewIssue.Reviewers = await GetReviewers(apiReviewIssue);

                        var feedback = new ApiReviewFeedback
                        {
                            Decision = decision,
                            Issue = apiReviewIssue,
                            FeedbackId = feedbackId,
                            FeedbackAuthor = feedbackAuthor,
                            FeedbackDateTime = feedbackDateTime,
                            FeedbackUrl = feedbackUrl,
                            FeedbackMarkdown = feedbackMarkdown,
                            VideoUrl = videoUrl
                        };
                        results.Add(feedback);
                    }
                }
            }

            results.Sort((x, y) => x.FeedbackDateTime.CompareTo(y.FeedbackDateTime));
            return results;
        }

        public async Task<IReadOnlyList<ApiReviewIssue>> GetIssuesAsync()
        {
            var repoList = _configuration["RepoList"];
            var repos = OrgAndRepo.ParseList(repoList).ToArray();

            var github = await _clientFactory.CreateForAppAsync();
            var result = new List<ApiReviewIssue>();

            foreach (var (owner, repo) in repos)
            {
                var request = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = ItemStateFilter.Open
                };
                request.Labels.Add(ApiReviewConstants.ApiReadyForReview);

                var issues = await github.Issue.GetAllForRepository(owner, repo, request);

                foreach (var issue in issues)
                {
                    var events = await github.Issue.Events.GetAllForIssue(owner, repo, issue.Number);
                    var apiReadyEvent = ApiReadyEvent.Get(events, issue.CreatedAt, DateTime.Now);

                    var apiReviewIssue = CreateIssue(owner, repo, issue);
                    apiReviewIssue.AreaOwner = apiReadyEvent?.DecisionMaker;
                    apiReviewIssue.Reviewers = await GetReviewers(apiReviewIssue);

                    result.Add(apiReviewIssue);
                }
            }

            result.Sort();

            return result;
        }

        private async Task<ApiReviewer[]> GetReviewers(ApiReviewIssue apiReviewIssue)
        {
            var linkSet = await _ospoService.GetLinkSetAsync();
            var result = new List<ApiReviewer>();

            Add(result, linkSet, apiReviewIssue.Author);
            foreach (var assignee in apiReviewIssue.Assignees ?? Array.Empty<string>())
                Add(result, linkSet, assignee);
            Add(result, linkSet, apiReviewIssue.AreaOwner);

            return result.ToArray();

            static void Add(List<ApiReviewer> target, OspoLinkSet linkSet, string userName)
            {
                if (userName == null)
                    return;

                if (target.Any(r => string.Equals(r.GitHubUserName, userName, StringComparison.OrdinalIgnoreCase)))
                    return;

                if (linkSet.LinkByLogin.TryGetValue(userName, out var link))
                {
                    var reviewer = new ApiReviewer
                    {
                        GitHubUserName = userName,
                        Name = link.MicrosoftInfo.PreferredName,
                        Email = link.MicrosoftInfo.EmailAddress
                    };
                    target.Add(reviewer);
                }
            }
        }

        private static ApiReviewIssue CreateIssue(string owner, string repo, Issue issue)
        {
            var result = new ApiReviewIssue
            {
                Owner = owner,
                Repo = repo,
                Author = issue.User.Login,
                Assignees = issue.Assignees.Select(a => a.Login).ToArray(),
                CreatedAt = issue.CreatedAt,
                Labels = issue.Labels.Select(l => new ApiReviewLabel { Name = l.Name, BackgroundColor = l.Color, Description = l.Description }).ToArray(),
                Milestone = issue.Milestone?.Title ?? ApiReviewConstants.NoMilestone,
                Title = GitHubIssueHelpers.FixTitle(issue.Title),
                Url = issue.HtmlUrl,
                Id = issue.Number
            };
            return result;
        }

        private sealed class ApiReadyEvent
        {
            public ApiReadyEvent(string decisionMaker, DateTimeOffset createdAt)
            {
                DecisionMaker = decisionMaker;
                CreatedAt = createdAt;
            }

            public string DecisionMaker { get; }
            public DateTimeOffset CreatedAt { get; }

            public static ApiReadyEvent Get(IEnumerable<EventInfo> events, DateTimeOffset start, DateTimeOffset end)
            {
                var readyEvent = default(EventInfo);

                foreach (var e in events.Where(e => e.CreatedAt <= end)
                                        .OrderByDescending(e => e.CreatedAt))
                {
                    switch (e.Event.StringValue)
                    {
                        case "labeled" when string.Equals(e.Label.Name, ApiReviewConstants.ApiReadyForReview, StringComparison.OrdinalIgnoreCase):
                            readyEvent = e;
                            break;
                    }
                }

                return readyEvent == null
                        ? null
                        : new ApiReadyEvent(readyEvent.Actor.Login, readyEvent.CreatedAt);
            }
        }

        private sealed class ApiReviewOutcome
        {
            public ApiReviewOutcome(ApiReviewDecision decision, string decisionMaker, DateTimeOffset decisionTime)
            {
                Decision = decision;
                DecisionMaker = decisionMaker;
                DecisionTime = decisionTime;
            }

            public static ApiReviewOutcome Get(IEnumerable<EventInfo> events, DateTimeOffset start, DateTimeOffset end)
            {
                var readyEvent = default(EventInfo);
                var current = default(ApiReviewOutcome);
                var rejection = default(ApiReviewOutcome);

                foreach (var e in events.Where(e => e.CreatedAt <= end)
                                        .OrderBy(e => e.CreatedAt))
                {
                    switch (e.Event.StringValue)
                    {
                        case "labeled" when string.Equals(e.Label.Name, ApiReviewConstants.ApiReadyForReview, StringComparison.OrdinalIgnoreCase):
                            current = null;
                            readyEvent = e;
                            break;
                        case "labeled" when string.Equals(e.Label.Name, ApiReviewConstants.ApiApproved, StringComparison.OrdinalIgnoreCase):
                            current = new ApiReviewOutcome(ApiReviewDecision.Approved, e.Actor.Login, e.CreatedAt);
                            readyEvent = null;
                            break;
                        case "labeled" when string.Equals(e.Label.Name, ApiReviewConstants.ApiNeedsWork, StringComparison.OrdinalIgnoreCase):
                            current = new ApiReviewOutcome(ApiReviewDecision.NeedsWork, e.Actor.Login, e.CreatedAt);
                            readyEvent = null;
                            break;
                        case "reopened":
                            rejection = null;
                            break;
                        case "closed":
                            if (readyEvent != null)
                                rejection = new ApiReviewOutcome(ApiReviewDecision.Rejected, e.Actor.Login, e.CreatedAt);
                            break;
                    }
                }

                if (rejection != null)
                    current = rejection;

                if (current != null)
                {
                    var inInterval = start <= current.DecisionTime && current.DecisionTime <= end;
                    if (!inInterval)
                        return null;
                }

                return current;
            }

            public ApiReviewDecision Decision { get; }
            public string DecisionMaker { get; }
            public DateTimeOffset DecisionTime { get; }
        }
    }
}
