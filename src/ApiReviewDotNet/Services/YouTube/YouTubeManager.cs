﻿using ApiReviewDotNet.Data;

using Google.Apis.YouTube.v3.Data;

using static Google.Apis.YouTube.v3.SearchResource.ListRequest;

namespace ApiReviewDotNet.Services.YouTube;

public class YouTubeManager
{
    private readonly YouTubeServiceFactory _youTubeServiceFactory;

    public YouTubeManager(YouTubeServiceFactory youTubeServiceFactory)
    {
        _youTubeServiceFactory = youTubeServiceFactory;
    }

    public async Task<ApiReviewVideo?> GetVideoAsync(string id)
    {
        var service = _youTubeServiceFactory.Create();

        var videoRequest = service.Videos.List("snippet,liveStreamingDetails");
        videoRequest.Id = id;
        var videoResponse = await videoRequest.ExecuteAsync();
        if (videoResponse.Items.Count == 0)
            return null;

        return CreateVideo(videoResponse.Items[0]);
    }

    public async Task<ApiReviewVideo?> GetVideoAsync(DateTimeOffset start, DateTimeOffset end)
    {
        var videos = await GetVideosAsync(start, end);
        return videos.FirstOrDefault();
    }

    public async Task<IReadOnlyList<ApiReviewVideo>> GetVideosAsync(DateTimeOffset start, DateTimeOffset end)
    {
        var service = _youTubeServiceFactory.Create();

        var result = new List<Video>();
        var nextPageToken = "";

        var searchRequest = service.Search.List("snippet");
        searchRequest.ChannelId = ApiReviewConstants.NetFoundationChannelId;
        searchRequest.Type = "video";
        searchRequest.EventType = EventTypeEnum.Completed;
        searchRequest.PublishedAfterDateTimeOffset = start;
        searchRequest.PublishedBeforeDateTimeOffset = end;
        searchRequest.MaxResults = 25;

        while (nextPageToken is not null)
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

        var videos = result.Where(v => v.LiveStreamingDetails is not null &&
                                       v.LiveStreamingDetails.ActualStartTimeDateTimeOffset is not null &&
                                       v.LiveStreamingDetails.ActualEndTimeDateTimeOffset is not null)
                           .Select(CreateVideo)
                           .OrderBy(v => v.StartDateTime);
        return videos.ToArray();
    }

    private static ApiReviewVideo CreateVideo(Video v)
    {
        return new ApiReviewVideo(v.Id,
                                  v.LiveStreamingDetails.ActualStartTimeDateTimeOffset!.Value,
                                  v.LiveStreamingDetails.ActualEndTimeDateTimeOffset!.Value,
                                  v.Snippet.Title,
                                  v.Snippet.Thumbnails.Medium.Url);
    }
}
