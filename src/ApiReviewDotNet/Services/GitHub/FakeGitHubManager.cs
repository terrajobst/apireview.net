using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using ApiReviewDotNet.Data;

namespace ApiReviewDotNet.Services.GitHub
{
    public sealed class FakeGitHubManager : IGitHubManager
    {
        private readonly IReadOnlyList<ApiReviewIssue> _issues;
        private readonly IReadOnlyList<ApiReviewFeedback> _feedback;

        public FakeGitHubManager()
        {
            _issues = JsonSerializer.Deserialize<IReadOnlyList<ApiReviewIssue>>(Resources.GitHubFakeIssues);
            _feedback = JsonSerializer.Deserialize<IReadOnlyList<ApiReviewFeedback>>(Resources.GitHubFakeFeedback);
        }

        public Task<IReadOnlyList<ApiReviewFeedback>> GetFeedbackAsync(IReadOnlyCollection<OrgAndRepo> repos, DateTimeOffset start, DateTimeOffset end)
        {
            var result = _feedback.Where(f => start <= f.FeedbackDateTime
                                              && f.FeedbackDateTime <= end
                                              && repos.Any(r => string.Equals(r.FullName, f.Issue.RepoFull, StringComparison.OrdinalIgnoreCase)))
                                  .ToArray();

            return Task.FromResult<IReadOnlyList<ApiReviewFeedback>>(result);
        }

        public Task<IReadOnlyList<ApiReviewIssue>> GetIssuesAsync()
        {
            return Task.FromResult(_issues);
        }
    }
}
