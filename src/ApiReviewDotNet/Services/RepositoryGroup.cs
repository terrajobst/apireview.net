using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace ApiReviewDotNet.Services
{
    public sealed class RepositoryGroup
    {
        public string Name { get; set; }
        public OrgAndRepo[] Repos { get; set; }

        public static IReadOnlyList<RepositoryGroup> Get(IConfiguration configuration)
        {
            var result = new List<RepositoryGroup>();

            foreach (var groupConfiguration in configuration.GetChildren())
            {
                var item = new RepositoryGroup
                {
                    Name = groupConfiguration.Key,
                    Repos = groupConfiguration.GetChildren()
                                              .Select(c => OrgAndRepo.Parse(c.Value))
                                              .ToArray()
                };

                result.Add(item);
            }

            return result.ToArray();
        }

        public override string ToString()
        {
            return $"{Name} ({string.Join(", ", Repos.AsEnumerable())})";
        }
    }
}
