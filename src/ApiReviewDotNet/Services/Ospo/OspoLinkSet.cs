namespace ApiReviewDotNet.Services.Ospo;

public sealed class OspoLinkSet
{
    public static OspoLinkSet Empty { get; } = new OspoLinkSet(Array.Empty<OspoLink>());

    public OspoLinkSet(IEnumerable<OspoLink> links)
    {
        Links = links.ToArray();
        LinkByLogin = Links.ToDictionary(l => l.GitHubInfo.Login);
    }

    public IReadOnlyList<OspoLink> Links { get; }

    public IReadOnlyDictionary<string, OspoLink> LinkByLogin { get; }
}
