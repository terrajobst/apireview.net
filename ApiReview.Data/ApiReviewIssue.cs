using System;
using System.Linq;

namespace ApiReview.Data
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

        public string DetailText => $"{IdFull} {CreatedAt.FormatAge()} by {Author}";

        public string Url { get; set; }

        public string Milestone { get; set; }

        public ApiReviewLabel[] Labels { get; set; }

        public bool IsBlocking => Labels != null && Labels.Any(l => string.Equals(l.Name, "blocking", StringComparison.OrdinalIgnoreCase));

        public int CompareTo(ApiReviewIssue other)
        {
            var result = -IsBlocking.CompareTo(other.IsBlocking);
            if (result == 0)
                result = CreatedAt.CompareTo(other.CreatedAt);

            return result;
        }
    }
}
