using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ApiReview.Data
{
    public sealed class ApiReviewSummary
    {
        public ApiReviewVideo Video { get; set; }
        public IReadOnlyList<ApiReviewFeedbackWithVideo> Items { get; set; }
    }
}
