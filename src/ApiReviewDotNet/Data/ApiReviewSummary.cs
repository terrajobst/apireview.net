using System.Collections.Generic;

namespace ApiReviewDotNet.Data
{
    public sealed class ApiReviewSummary
    {
        public ApiReviewVideo Video { get; set; }
        public IReadOnlyList<ApiReviewFeedbackWithVideo> Items { get; set; }
    }
}
