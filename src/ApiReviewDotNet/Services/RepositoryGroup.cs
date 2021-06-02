using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace ApiReviewDotNet.Services
{
    public sealed class RepositoryGroup
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsDefault { get; set; }
        public OrgAndRepo[] Repos { get; set; }
        public string MailingList { get; set; }
        public string MailingReplyTo { get; set; }
        public OrgAndRepo NotesRepo { get; set; }
        public string NotesSuffix { get; set; }

        public static IReadOnlyList<RepositoryGroup> Get(IConfiguration configuration)
        {
            var result = new List<RepositoryGroup>();

            foreach (var groupConfiguration in configuration.GetChildren())
            {
                var item = new RepositoryGroup
                {
                    Name = groupConfiguration.Key,
                    DisplayName = groupConfiguration["DisplayName"],
                    IsDefault = groupConfiguration.GetValue("IsDefault", false),
                    Repos = groupConfiguration.GetSection("Repos")
                                              .GetChildren()
                                              .Select(r => OrgAndRepo.Parse(r.Value))
                                              .ToArray(),
                    MailingList = groupConfiguration["MailingList"],
                    MailingReplyTo = groupConfiguration["MailingReplyTo"],
                    NotesRepo = OrgAndRepo.Parse(groupConfiguration["NotesRepo"]),
                    NotesSuffix = groupConfiguration["NotesSuffix"]
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
