using Octokit;

namespace ApiReviewDotNet.Services.GitHub;

public sealed class GitHubTeamService
{
    private static readonly TimeSpan _refreshInterval = TimeSpan.FromHours(1);
    private readonly ILogger<GitHubTeamService> _logger;
    private readonly GitHubClientFactory _clientFactory;
    private readonly string[] _orgs;
    private Dictionary<string, IReadOnlyList<string>> _membersByTeam = new();

    public GitHubTeamService(ILogger<GitHubTeamService> logger,
                             GitHubClientFactory clientFactory,
                             RepositoryGroupService repositoryGroupService)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _orgs = repositoryGroupService.Repositories.Select(r => r.OrgName).Distinct().ToArray();
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
            var github = await _clientFactory.CreateForAppAsync();
            _membersByTeam = await LoadTeams(github);
            _logger.LogInformation("Loaded {count} teams", _membersByTeam.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading teams");
        }
    }

    private async Task<Dictionary<string, IReadOnlyList<string>>> LoadTeams(GitHubClient github)
    {
        var membersByTeam = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var org in _orgs)
        {
            var teams = await github.Organization.Team.GetAll(org);

            foreach (var team in teams)
            {
                var members = await github.Organization.Team.GetAllMembers(team.Id);
                var memberNames = members.Select(u => u.Login).ToArray();

                if (membersByTeam.TryGetValue(team.Name, out var existingMembers))
                    memberNames = memberNames.Concat(existingMembers).Distinct().Order().ToArray();

                membersByTeam[team.Name] = memberNames;
            }
        }

        return membersByTeam;
    }

    public IReadOnlyList<string>? GetMembers(string teamName)
    {
        _membersByTeam.TryGetValue(teamName, out var result);
        return result;
    }
}
