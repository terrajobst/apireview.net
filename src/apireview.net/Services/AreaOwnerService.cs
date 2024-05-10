using ApiReviewDotNet.Services.GitHub;

namespace ApiReviewDotNet.Services;

public sealed class AreaOwnerService
{
    private readonly ILogger<AreaOwnerService> _logger;
    private readonly GitHubTeamService _teamService;
    private Dictionary<string, string[]> _ownerByArea = new();

    public AreaOwnerService(ILogger<AreaOwnerService> logger,
                            GitHubTeamService teamService)
    {
        _logger = logger;
        _teamService = teamService;
    }

    public IReadOnlyList<string> GetOwners(string area)
    {
        if (!_ownerByArea.TryGetValue(area, out var result))
            result = Array.Empty<string>();

        return result;
    }

    public async Task ReloadAsync()
    {
        try
        {
            _ownerByArea = await GetOwnersAsync(_teamService);
            _logger.LogInformation("Loaded {count} area owners", _ownerByArea.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading area owners");
        }
    }

    private static async Task<Dictionary<string, string[]>> GetOwnersAsync(GitHubTeamService teamService)
    {
        var url = "https://raw.githubusercontent.com/dotnet/runtime/main/docs/area-owners.md";
        var client = new HttpClient();
        var contents = await client.GetStringAsync(url);
        var lines = GetLines(contents);
        var result = new Dictionary<string, string[]>();

        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length != 6)
                continue;

            var area = parts[1].Trim();
            var ownerText = parts[3].Trim();
            var owners = ownerText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            owners = owners.Select(o => o.Replace("@", "").Trim()).ToArray();

            if (!area.StartsWith("area-", StringComparison.OrdinalIgnoreCase))
                continue;

            var expandedOwners = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var owner in owners)
            {
                var members = teamService.GetMembers(owner);
                if (members is not null)
                    expandedOwners.UnionWith(members);
                else
                    expandedOwners.Add(owner);
            }

            result[area] = expandedOwners.ToArray();
        }

        return result;
    }

    private static IEnumerable<string> GetLines(string text)
    {
        using var stringReader = new StringReader(text);
        while (true)
        {
            var line = stringReader.ReadLine();
            if (line is null)
                yield break;

            yield return line;
        }
    }
}
