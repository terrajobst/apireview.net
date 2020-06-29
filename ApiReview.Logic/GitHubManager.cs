using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using ApiReview.Data;

using Octokit;

namespace ApiReview.Logic
{
    public interface IGitHubManager
    {
        Task<IReadOnlyList<ApiReviewFeedback>> GetFeedbackAsync(DateTimeOffset start, DateTimeOffset end);
        Task<IReadOnlyList<ApiReviewIssue>> GetIssuesAsync();
    }

    public sealed class FakeGitHubManager : IGitHubManager
    {
        private IReadOnlyList<ApiReviewIssue> _issues;
        private IReadOnlyList<ApiReviewFeedback> _feedback;

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
        private const string _repoList = "dotnet/runtime,dotnet/winforms";

        public Task<IReadOnlyList<ApiReviewFeedback>> GetFeedbackAsync(DateTimeOffset start, DateTimeOffset end)
        {
            var repos = OrgAndRepo.ParseList(_repoList).ToArray();
            return GetFeedbackAsync(repos, start, end);
        }

        private static async Task<IReadOnlyList<ApiReviewFeedback>> GetFeedbackAsync(OrgAndRepo[] repos, DateTimeOffset start, DateTimeOffset end)
        {
            static string GetApiStatus(Issue issue)
            {
                var isReadyForReview = issue.Labels.Any(l => l.Name == "api-ready-for-review");
                var isApproved = issue.Labels.Any(l => l.Name == "api-approved");
                var needsWork = issue.Labels.Any(l => l.Name == "api-needs-work");
                var isRejected = isReadyForReview && issue.State.Value == ItemState.Closed;

                var isApi = isApproved || needsWork || isRejected;

                if (!isApi)
                    return null;

                if (isApproved)
                    return "Approved";

                if (isRejected)
                    return "Rejected";

                return "Needs Work";
            }

            static bool WasEverReadyForReview(Issue issue, IEnumerable<EventInfo> events)
            {
                if (issue.Labels.Any(l => l.Name == "api-ready-for-review" ||
                                          l.Name == "api-approved"))
                    return true;

                foreach (var eventInfo in events)
                {
                    if (eventInfo.Label?.Name == "api-ready-for-review" ||
                        eventInfo.Label?.Name == "api-approved")
                        return true;
                }

                return false;
            }

            static bool IsApiEvent(EventInfo eventInfo)
            {
                // We need to work around unsupported enum values:
                // - https://github.com/octokit/octokit.net/issues/2023
                // - https://github.com/octokit/octokit.net/issues/2025
                //
                // which will cause Value to throw an exception.

                switch (eventInfo.Event.StringValue)
                {
                    case "labeled":
                        if (eventInfo.Label.Name == "api-approved" || eventInfo.Label.Name == "api-needs-work")
                            return true;
                        break;
                    case "closed":
                        return true;
                }

                return false;
            }

            static IEnumerable<EventInfo> GetApiEvents(IEnumerable<EventInfo> events, DateTimeOffset start, DateTimeOffset end)
            {
                foreach (var eventGroup in events.Where(e => start <= e.CreatedAt && e.CreatedAt <= end && IsApiEvent(e))
                                                 .GroupBy(e => e.CreatedAt.Date))
                {
                    var latest = eventGroup.OrderBy(e => e.CreatedAt).Last();
                    yield return latest;
                }
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

            var github = GitHubClientFactory.Create();
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
                    var status = GetApiStatus(issue);
                    if (status == null)
                        continue;

                    var events = await github.Issue.Events.GetAllForIssue(owner, repo, issue.Number);

                    if (!WasEverReadyForReview(issue, events))
                        continue;

                    foreach (var apiEvent in GetApiEvents(events, start, end))
                    {
                        var title = GitHubIssueHelpers.FixTitle(issue.Title);
                        var feedbackDateTime = apiEvent.CreatedAt;
                        var comments = await github.Issue.Comment.GetAllForIssue(owner, repo, issue.Number);
                        var eventComment = comments.Where(c => c.User.Login == apiEvent.Actor.Login)
                                                   .Select(c => (comment: c, within: Math.Abs((c.CreatedAt - feedbackDateTime).TotalSeconds)))
                                                   .Where(c => c.within <= TimeSpan.FromMinutes(15).TotalSeconds)
                                                   .OrderBy(c => c.within)
                                                   .Select(c => c.comment)
                                                   .FirstOrDefault();
                        var feedbackId = eventComment?.Id;
                        var feedbackUrl = eventComment?.HtmlUrl ?? issue.HtmlUrl;
                        var (videoUrl, feedbackMarkdown) = ParseFeedback(eventComment?.Body);

                        var apiReviewIssue = CreateIssue(owner, repo, issue);

                        var feedback = new ApiReviewFeedback
                        {
                            Issue = apiReviewIssue,
                            FeedbackId = feedbackId,
                            FeedbackDateTime = feedbackDateTime,
                            FeedbackUrl = feedbackUrl,
                            FeedbackStatus = status,
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
            var repos = OrgAndRepo.ParseList(_repoList).ToArray();

            var github = GitHubClientFactory.Create();
            var result = new List<ApiReviewIssue>();

            foreach (var (owner, repo) in repos)
            {
                var request = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = ItemStateFilter.Open
                };
                request.Labels.Add("api-ready-for-review");

                var issues = await github.Issue.GetAllForRepository(owner, repo, request);

                foreach (var issue in issues)
                {
                    var apiReviewIssue = CreateIssue(owner, repo, issue);
                    result.Add(apiReviewIssue);
                }
            }

            result.Sort();

            return result;
        }

        private static ApiReviewIssue CreateIssue(string owner, string repo, Issue issue)
        {
            var result = new ApiReviewIssue
            {
                Owner = owner,
                Repo = repo,
                Author = issue.User.Login,
                CreatedAt = issue.CreatedAt,
                Labels = issue.Labels.Select(l => new ApiReviewLabel { Name = l.Name, BackgroundColor = l.Color, Description = l.Description }).ToArray(),
                Milestone = issue.Milestone?.Title ?? "(None)",
                Title = GitHubIssueHelpers.FixTitle(issue.Title),
                Url = issue.HtmlUrl,
                Id = issue.Number
            };
            return result;
        }
    }
}
