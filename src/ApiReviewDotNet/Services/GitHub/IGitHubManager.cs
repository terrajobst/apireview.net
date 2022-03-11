using ApiReviewDotNet.Data;

namespace ApiReviewDotNet.Services.GitHub;

public interface IGitHubManager
{
    Task<IReadOnlyList<ApiReviewItem>> GetFeedbackAsync(IReadOnlyCollection<OrgAndRepo> repos, DateTimeOffset start, DateTimeOffset end);
    Task<IReadOnlyList<ApiReviewIssue>> GetIssuesAsync();
}
