using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ApiReview.Shared;

using Google.Apis.YouTube.v3.Data;

using static Google.Apis.YouTube.v3.SearchResource.ListRequest;

namespace ApiReview.Server.Logic
{
    public interface IYouTubeManager
    {
        Task<ApiReviewVideo> GetVideoAsync(string id);
        Task<ApiReviewVideo> GetVideoAsync(DateTimeOffset start, DateTimeOffset end);
        Task<IReadOnlyList<ApiReviewVideo>> GetVideosAsync(DateTimeOffset start, DateTimeOffset end);
    }

    public class FakeYouTubeManager : IYouTubeManager
    {
        private IReadOnlyList<ApiReviewVideo> GetVideos()
        {
            return new[]
            {
                new ApiReviewVideo(
                    "rx_098IdZU0",
                    DateTimeOffset.Parse("2020-06-25T16:54:00Z"),
                    DateTimeOffset.Parse("2020-06-25T19:02:22Z"),
                    "GitHub Quick Reviews",
                    "https://i.ytimg.com/vi/rx_098IdZU0/mqdefault.jpg"
                ),
                new ApiReviewVideo(
                    "q7ODj3RJnME",
                    DateTimeOffset.Parse("2020-06-25T16:53:18Z"),
                    DateTimeOffset.Parse("2020-06-25T17:42:57Z"),
                    "Desktop: .NET Community Standup - June 25th 2020 - New XAML Desktop Features",
                    "https://i.ytimg.com/vi/q7ODj3RJnME/mqdefault.jpg"
                ),
                new ApiReviewVideo(
                    "R5G4scTRRNQ",
                    DateTimeOffset.Parse("2020-06-26T17:00:12Z"),
                    DateTimeOffset.Parse("2020-06-26T18:55:24Z"),
                    "GitHub Quick Reviews",
                    "https://i.ytimg.com/vi/R5G4scTRRNQ/mqdefault.jpg"
                )
            };
        }

        public Task<ApiReviewVideo> GetVideoAsync(string id)
        {
            var videos = GetVideos();
            var result = videos.FirstOrDefault(v => v.Id == id);
            return Task.FromResult(result);
        }

        public async Task<ApiReviewVideo> GetVideoAsync(DateTimeOffset start, DateTimeOffset end)
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

    public class YouTubeManager : IYouTubeManager
    {
        private const string _netFoundationChannelId = "UCiaZbznpWV1o-KLxj8zqR6A";

        public async Task<ApiReviewVideo> GetVideoAsync(string id)
        {
            var service = await YouTubeServiceFactory.CreateAsync();

            var videoRequest = service.Videos.List("snippet,liveStreamingDetails");
            videoRequest.Id = id;
            var videoResponse = await videoRequest.ExecuteAsync();
            if (videoResponse.Items.Count == 0)
                return null;

            return CreateVideo(videoResponse.Items[0]);
        }

        public async Task<ApiReviewVideo> GetVideoAsync(DateTimeOffset start, DateTimeOffset end)
        {
            var videos = await GetVideosAsync(start, end);
            return videos.FirstOrDefault();
        }

        public async Task<IReadOnlyList<ApiReviewVideo>> GetVideosAsync(DateTimeOffset start, DateTimeOffset end)
        {
            var service = await YouTubeServiceFactory.CreateAsync();

            var result = new List<Video>();
            var nextPageToken = "";

            var searchRequest = service.Search.List("snippet");
            searchRequest.ChannelId = _netFoundationChannelId;
            searchRequest.Type = "video";
            searchRequest.EventType = EventTypeEnum.Completed;
            searchRequest.PublishedAfter = start.DateTime;
            searchRequest.PublishedBefore = end.DateTime;
            searchRequest.MaxResults = 25;

            while (nextPageToken != null)
            {
                searchRequest.PageToken = nextPageToken;
                var response = await searchRequest.ExecuteAsync();

                var ids = response.Items.Select(i => i.Id.VideoId);
                var idString = string.Join(",", ids);

                var videoRequest = service.Videos.List("snippet,liveStreamingDetails");
                videoRequest.Id = idString;
                var videoResponse = await videoRequest.ExecuteAsync();
                result.AddRange(videoResponse.Items);

                nextPageToken = response.NextPageToken;
            }

            var videos = result.Where(v => v.LiveStreamingDetails != null &&
                                           v.LiveStreamingDetails.ActualStartTime != null &&
                                           v.LiveStreamingDetails.ActualEndTime != null)
                               .Select(CreateVideo)
                               .OrderByDescending(v => v.Duration);
            return videos.ToArray();
        }

        private static ApiReviewVideo CreateVideo(Video v)
        {
            return new ApiReviewVideo(v.Id,
                                      v.LiveStreamingDetails.ActualStartTime.Value,
                                      v.LiveStreamingDetails.ActualEndTime.Value,
                                      v.Snippet.Title,
                                      v.Snippet.Thumbnails?.Medium?.Url);
        }
    }
}
