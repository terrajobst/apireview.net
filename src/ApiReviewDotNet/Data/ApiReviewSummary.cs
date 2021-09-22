namespace ApiReviewDotNet.Data;

public sealed class ApiReviewSummary
{
    public ApiReviewSummary(string repositoryGroup,
                            ApiReviewVideo? video,
                            IReadOnlyList<ApiReviewFeedbackWithVideo> items)
    {
        RepositoryGroup = repositoryGroup;
        Video = video;
        Items = items;
    }

    public string RepositoryGroup { get; }

    public ApiReviewVideo? Video { get; }

    public IReadOnlyList<ApiReviewFeedbackWithVideo> Items { get; }
}
