namespace ApiReviewDotNet.Data
{
    public sealed class ApiReviewFeedback
    {
        public ApiReviewDecision Decision { get; set; }
        public ApiReviewIssue Issue { get; set; }
        public DateTimeOffset FeedbackDateTime { get; set; }
        public string FeedbackId { get; set; }
        public string FeedbackAuthor { get; set; }
        public string FeedbackUrl { get; set; }
        public string FeedbackMarkdown { get; set; }
        public string VideoUrl { get; set; }
    }
}
