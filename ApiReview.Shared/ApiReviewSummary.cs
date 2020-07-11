using System.Collections.Generic;

namespace ApiReview.Shared
{
    public sealed class ApiReviewSummary
    {
        public ApiReviewVideo Video { get; set; }
        public IReadOnlyList<ApiReviewFeedbackWithVideo> Items { get; set; }
    }
}
