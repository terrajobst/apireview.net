using ApiReviewDotNet.Data;

namespace ApiReviewDotNet.Services.YouTube;

public interface IYouTubeManager
{
    Task<ApiReviewVideo?> GetVideoAsync(string id);
    Task<ApiReviewVideo?> GetVideoAsync(DateTimeOffset start, DateTimeOffset end);
    Task<IReadOnlyList<ApiReviewVideo>> GetVideosAsync(DateTimeOffset start, DateTimeOffset end);
}
