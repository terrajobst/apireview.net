namespace ApiReviewDotNet.Services
{
    public sealed class RepositoryGroupService
    {
        public RepositoryGroupService(IConfiguration configuration)
        {
            RepositoryGroups = RepositoryGroup.Get(configuration.GetSection("RepositoryGroups"));
            Repositories = RepositoryGroups.SelectMany(rg => rg.Repos.Select(r => r.FullName))
                                           .Distinct(StringComparer.OrdinalIgnoreCase)
                                           .Select(r => OrgAndRepo.Parse(r)!)
                                           .ToArray();
            ApproverTeamSlugs = RepositoryGroups.Select(rg => rg.ApproverTeamSlug)
                                                .Distinct()
                                                .ToArray();
        }

        public RepositoryGroup? Get(string name)
        {
            return RepositoryGroups.FirstOrDefault(rg => string.Equals(rg.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public RepositoryGroup Default => RepositoryGroups.Single(r => r.IsDefault);
        public IReadOnlyList<RepositoryGroup> RepositoryGroups { get; }
        public IReadOnlyList<OrgAndRepo> Repositories { get; }
        public IReadOnlyList<string> ApproverTeamSlugs { get; }
    }
}
