namespace ApiReview.Shared
{
    public sealed class ApiReviewPublicationResult
    {
        public static ApiReviewPublicationResult Failed() => new ApiReviewPublicationResult { Success = false, Url = null };
        public static ApiReviewPublicationResult Suceess(string url) => new ApiReviewPublicationResult { Success = true, Url = url };

        public bool Success { get; set; }
        public string Url { get; set; }
    }
}
