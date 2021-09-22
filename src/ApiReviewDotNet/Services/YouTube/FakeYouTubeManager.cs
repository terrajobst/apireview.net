using ApiReviewDotNet.Data;

namespace ApiReviewDotNet.Services.YouTube;

public class FakeYouTubeManager : IYouTubeManager
{
    private static IReadOnlyList<ApiReviewVideo> GetVideos()
    {
        return new[]
        {
                new ApiReviewVideo(
                    "q7ODj3RJnME",
                    DateTime.Parse("2020-06-25T16:53:18Z"),
                    DateTime.Parse("2020-06-25T17:42:57Z"),
                    "Desktop: .NET Community Standup - June 25th 2020 - New XAML Desktop Features",
                    "https://i.ytimg.com/vi/q7ODj3RJnME/mqdefault.jpg"
                ),
                new ApiReviewVideo(
                    "rx_098IdZU0",
                    DateTime.Parse("2020-06-25T16:54:00Z"),
                    DateTime.Parse("2020-06-25T19:02:22Z"),
                    "GitHub Quick Reviews",
                    "https://i.ytimg.com/vi/rx_098IdZU0/mqdefault.jpg"
                ),
                new ApiReviewVideo(
                    "R5G4scTRRNQ",
                    DateTime.Parse("2020-06-26T17:00:12Z"),
                    DateTime.Parse("2020-06-26T18:55:24Z"),
                    "GitHub Quick Reviews",
                    "https://i.ytimg.com/vi/R5G4scTRRNQ/mqdefault.jpg"
                )
            };
    }

    public Task<ApiReviewVideo?> GetVideoAsync(string id)
    {
        var videos = GetVideos();
        var result = videos.FirstOrDefault(v => v.Id == id);
        return Task.FromResult(result);
    }

    public async Task<ApiReviewVideo?> GetVideoAsync(DateTimeOffset start, DateTimeOffset end)
    {
        var videos = await GetVideosAsync(start, end);
        var result = videos.FirstOrDefault();
        return result;
    }

    public Task<IReadOnlyList<ApiReviewVideo>> GetVideosAsync(DateTimeOffset start, DateTimeOffset end)
    {
        var videos = GetVideos();
        var result = videos.Where(v => start <= v.StartDateTime && v.EndDateTime <= end)
                           .ToArray();

        return Task.FromResult<IReadOnlyList<ApiReviewVideo>>(result);
    }
}
