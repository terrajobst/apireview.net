using ApiReviewDotNet.Services.GitHub;
using ApiReviewDotNet.Services.Ospo;

namespace ApiReviewDotNet.Services;

public sealed class RefreshService
{
    private static readonly TimeSpan _refreshInterval = TimeSpan.FromHours(1);

    private readonly ILogger<RefreshService> _logger;
    private readonly OspoService _ospoService;
    private readonly GitHubTeamService _teamService;
    private readonly AreaOwnerService _areaOwnerService;
    private readonly IssueService _issueService;

    public RefreshService(ILogger<RefreshService> logger,
                          OspoService ospoService,
                          GitHubTeamService teamService,
                          AreaOwnerService areaOwnerService,
                          IssueService issueService)
    {
        _logger = logger;
        _ospoService = ospoService;
        _teamService = teamService;
        _areaOwnerService = areaOwnerService;
        _issueService = issueService;
    }

    public async Task StartAsync()
    {
        await ReloadAsync();

        _ = Task.Run(async () => {
            await Task.Delay(_refreshInterval);
            await ReloadAsync();
        });
    }

    public async Task ReloadAsync()
    {
        try
        {
            //await _ospoService.ReloadAsync();
            await _teamService.ReloadAsync();
            await _areaOwnerService.ReloadAsync();
            await _issueService.ReloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing data");
        }
    }
}
