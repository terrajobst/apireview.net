using Octokit;

namespace ApiReviewDotNet.Services.GitHub;

public sealed class GitHubTeamService
{
    private readonly ILogger<GitHubTeamService> _logger;
    private readonly GitHubClientFactory _clientFactory;
    private readonly string[] _orgs;
    private Dictionary<string, IReadOnlyList<string>> _membersBySlug = new();

    public GitHubTeamService(ILogger<GitHubTeamService> logger,
                             GitHubClientFactory clientFactory,
                             RepositoryGroupService repositoryGroupService)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _orgs = repositoryGroupService.Repositories.Select(r => r.OrgName).Distinct().ToArray();
    }

    public async Task ReloadAsync()
    {
        try
        {
            var github = await _clientFactory.CreateForAppAsync();
            _membersBySlug = await LoadTeams(github);
            _logger.LogInformation("Loaded {count} teams", _membersBySlug.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading teams");
        }
    }

    private async Task<Dictionary<string, IReadOnlyList<string>>> LoadTeams(GitHubClient github)
    {
        var membersBySlug = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var org in _orgs)
        {
            var teams = await github.Organization.Team.GetAll(org);

            foreach (var team in teams)
            {
                var slug = $"{org}/{team.Slug}";
                var members = await github.Organization.Team.GetAllMembers(team.Id);
                var memberNames = members.Select(u => u.Login).ToArray();
                membersBySlug.Add(slug, memberNames);
            }
        }

        return membersBySlug;
    }

    public IReadOnlyList<string>? GetMembers(string slug)
    {
        _membersBySlug.TryGetValue(slug, out var result);
        return result;
    }
}
