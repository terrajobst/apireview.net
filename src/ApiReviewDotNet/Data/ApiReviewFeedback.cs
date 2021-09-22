namespace ApiReviewDotNet.Data;

public sealed class ApiReviewFeedback
{
    public ApiReviewFeedback(ApiReviewDecision decision,
                             ApiReviewIssue issue,
                             DateTimeOffset feedbackDateTime,
                             string? feedbackId,
                             string? feedbackAuthor,
                             string feedbackUrl,
                             string? feedbackMarkdown,
                             string? videoUrl)
    {
        Decision = decision;
        Issue = issue;
        FeedbackDateTime = feedbackDateTime;
        FeedbackId = feedbackId;
        FeedbackAuthor = feedbackAuthor;
        FeedbackUrl = feedbackUrl;
        FeedbackMarkdown = feedbackMarkdown;
        VideoUrl = videoUrl;
    }

    public ApiReviewDecision Decision { get; }
    public ApiReviewIssue Issue { get; }
    public DateTimeOffset FeedbackDateTime { get; }
    public string? FeedbackId { get; }
    public string? FeedbackAuthor { get; }
    public string FeedbackUrl { get; }
    public string? FeedbackMarkdown { get; }
    public string? VideoUrl { get; }
}
