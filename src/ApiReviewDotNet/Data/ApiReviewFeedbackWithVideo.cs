namespace ApiReviewDotNet.Data;

public sealed class ApiReviewFeedbackWithVideo
{
    public ApiReviewFeedbackWithVideo(ApiReviewFeedback feedback,
                                      ApiReviewVideo? video,
                                      TimeSpan videoTimeCode)
    {
        Feedback = feedback;
        Video = video;
        VideoTimeCode = videoTimeCode;
    }

    public ApiReviewFeedback Feedback { get; }
    public ApiReviewVideo? Video { get; }
    public TimeSpan VideoTimeCode { get; }

    public string? VideoTimeCodeUrl
    {
        get
        {
            if (Video == null)
                return null;

            var timeCodeText = $"{VideoTimeCode.Hours}h{VideoTimeCode.Minutes}m{VideoTimeCode.Seconds}s";
            return $"https://www.youtube.com/watch?v={Video.Id}&t={timeCodeText}";
        }
    }

    public override string ToString()
    {
        return $"{Feedback.Issue.Id} - {Feedback.Issue.Title} @ {VideoTimeCode}";
    }
}
