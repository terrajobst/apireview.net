using ApiReviewDotNet.Data;
using ApiReviewDotNet.Services.GitHub;

namespace ApiReviewDotNet.Services;

public sealed class IssueService
{
    private readonly GitHubManager _gitHubManager;

    public IssueService(GitHubManager gitHubManager)
    {
        _gitHubManager = gitHubManager;
        Load();
    }

    private async void Load()
    {
        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        Issues = await _gitHubManager.GetIssuesAsync();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<ApiReviewIssue> Issues { get; private set; } = Array.Empty<ApiReviewIssue>();

    public event EventHandler? Changed;
}
