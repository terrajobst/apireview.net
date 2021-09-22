namespace ApiReviewDotNet.Data
{
    public sealed class ApiReviewSummary
    {
        public string RepositoryGroup { get; set; }
        public ApiReviewVideo Video { get; set; }
        public IReadOnlyList<ApiReviewFeedbackWithVideo> Items { get; set; }
    }
}
