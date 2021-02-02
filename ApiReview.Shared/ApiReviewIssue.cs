using System;
using System.Linq;

namespace ApiReview.Shared
{
    public sealed class ApiReviewIssue : IComparable<ApiReviewIssue>
    {
        public string Owner { get; set; }

        public string Repo { get; set; }

        public int Id { get; set; }

        public string IdFull => $"{Owner}/{Repo}#{Id}";

        public string Title { get; set; }

        public string Author { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string DetailText => $"{IdFull} {CreatedAt.FormatRelative()} by {Author}";

        public string Url { get; set; }

        public string Milestone { get; set; }

        public ApiReviewLabel[] Labels { get; set; }

        public bool IsBlocking => Labels != null && Labels.Any(l => string.Equals(l.Name, ApiReviewConstants.Blocking, StringComparison.OrdinalIgnoreCase));

        public int CompareTo(ApiReviewIssue other)
        {
            var result = -IsBlocking.CompareTo(other.IsBlocking);
            if (result != 0)
                return result;

            result = CompareMilestone(Milestone, other.Milestone);
            if (result != 0)
                return result;

            return CreatedAt.CompareTo(other.CreatedAt);
        }

        private static int CompareMilestone(string x, string y)
        {
            // The desired sort order is:
            //
            // 1. Milestones that look like versions, sorted by version number
            // 2. Milestones that aren't versions, sorted by text (e.g. "Backlog", "Future")
            // 3. No milestone
            //
            // Why does no milestone go last? Because we don't to punish folks for triaging milestones.

            static bool IsNone(string m) => string.IsNullOrEmpty(m) || m == ApiReviewConstants.NoMilestone;

            if (IsNone(x) && IsNone(y))
                return 0;

            if (IsNone(x))
                return 1;

            if (IsNone(y))
                return -1;

            var xIsVersion = Version.TryParse(x, out var xVersion);
            var yIsVersion = Version.TryParse(y, out var yVersion);

            var result = -xIsVersion.CompareTo(yIsVersion);
            if (result != 0)
                return result;

            if (xIsVersion && yIsVersion)
                return xVersion.CompareTo(yVersion);

            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }
    }
}
