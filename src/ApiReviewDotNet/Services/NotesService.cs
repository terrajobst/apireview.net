﻿using ApiReviewDotNet.Data;
using ApiReviewDotNet.Services.YouTube;

namespace ApiReviewDotNet.Services;

public sealed class NotesService
{
    private readonly ILogger<NotesService> _logger;
    private readonly YouTubeManager _youTubeManager;
    private readonly SummaryManager _summaryManager;
    private readonly SummaryPublishingService _summaryPublishingService;

    public NotesService(ILogger<NotesService> logger,
                        YouTubeManager youTubeManager,
                        SummaryManager summaryManager,
                        SummaryPublishingService summaryPublishingService)
    {
        _logger = logger;
        _youTubeManager = youTubeManager;
        _summaryManager = summaryManager;
        _summaryPublishingService = summaryPublishingService;
    }

    public Task<IReadOnlyList<ApiReviewVideo>> GetVideos(DateTimeOffset start, DateTimeOffset end)
    {
        return _youTubeManager.GetVideosAsync(start, end);
    }

    public Task<ApiReviewVideo?> GetVideo(string videoId)
    {
        return _youTubeManager.GetVideoAsync(videoId);
    }

    public Task<ApiReviewSummary> IssuesForRange(RepositoryGroup repositoryGroup, DateTimeOffset start, DateTimeOffset end)
    {
        return _summaryManager.GetSummaryAsync(repositoryGroup, start, end);
    }

    public Task<ApiReviewSummary?> IssuesForVideo(RepositoryGroup repositoryGroup, string videoId)
    {
        return _summaryManager.GetSummaryAsync(repositoryGroup, videoId);
    }

    public async Task<ApiReviewPublicationResult?> PublishNotesAsync(ApiReviewSummary? summary)
    {
        if (summary?.Items?.Any() != true)
            return null;

        try
        {
            return await _summaryPublishingService.PublishAsync(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Can't publish notes");
            return ApiReviewPublicationResult.Failed();
        }
    }
}
