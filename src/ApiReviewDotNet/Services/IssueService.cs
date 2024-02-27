using ApiReviewDotNet.Data;
using ApiReviewDotNet.Services.GitHub;
using ApiReviewDotNet.Services.Ospo;

namespace ApiReviewDotNet.Services;

public sealed class IssueService : IDisposable
{
    private readonly GitHubManager _gitHubManager;
    private readonly AreaOwnerService _areaOwnerService;
    private readonly OspoService _ospoService;

    public IssueService(GitHubManager gitHubManager,
                        AreaOwnerService areaOwnerService,
                        OspoService ospoService)
    {
        _gitHubManager = gitHubManager;
        _areaOwnerService = areaOwnerService;
        _ospoService = ospoService;
        Load();

        _ospoService.Changed += OspoServiceChanged;
        _areaOwnerService.Changed += AreaOwnerServiceChanged;
    }

    public void Dispose()
    {
        _ospoService.Changed -= OspoServiceChanged;
        _areaOwnerService.Changed -= AreaOwnerServiceChanged;
    }

    private void OspoServiceChanged(object? sender, EventArgs e)
    {
        Load();
    }

    private void AreaOwnerServiceChanged(object? sender, EventArgs e)
    {
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
