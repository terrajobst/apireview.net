using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ApiReview.Shared;

namespace ApiReview.Server.Services
{
    public class SummaryManager
    {
        // We sometimes post the comments from the last issue slightly after the stream
        // has ended. To account for that, we'll give us 15 minutes extra.
        private static readonly TimeSpan _extraTimeAfterStreamEnded = TimeSpan.FromMinutes(15);

        private readonly IYouTubeManager _youTubeManager;
        private readonly IGitHubManager _gitHubManager;

        public SummaryManager(IYouTubeManager youTubeManager, IGitHubManager gitHubManager)
        {
            _youTubeManager = youTubeManager;
            _gitHubManager = gitHubManager;
        }

        public async Task<ApiReviewSummary> GetSummaryAsync(DateTimeOffset start, DateTimeOffset end)
        {
            var items = await _gitHubManager.GetFeedbackAsync(start, end);
            return CreateSummary(null, items);
        }

        public async Task<ApiReviewSummary> GetSummaryAsync(string videoId)
        {
            var video = await _youTubeManager.GetVideoAsync(videoId);
            if (video == null)
                return null;

            var start = video.StartDateTime;
            var end = video.EndDateTime + _extraTimeAfterStreamEnded;
            var items = await _gitHubManager.GetFeedbackAsync(start, end);
            return CreateSummary(video, items);
        }

        private static ApiReviewSummary CreateSummary(ApiReviewVideo video, IReadOnlyList<ApiReviewFeedback> items)
        {
            if (items.Count == 0)
            {
                return new ApiReviewSummary
                {
                    Video = video,
                    Items = Array.Empty<ApiReviewFeedbackWithVideo>()
                };
            }
            else
            {
                var result = new List<ApiReviewFeedbackWithVideo>();
                var reviewStart = video == null
                                    ? items.OrderBy(i => i.FeedbackDateTime).Select(i => i.FeedbackDateTime).First()
                                    : video.StartDateTime;

                var reviewEnd = video == null
                                    ? items.OrderBy(i => i.FeedbackDateTime).Select(i => i.FeedbackDateTime).Last()
                                    : video.EndDateTime + _extraTimeAfterStreamEnded;

                for (var i = 0; i < items.Count; i++)
                {
                    var current = items[i];

                    if (video != null)
                    {
                        var wasDuringReview = reviewStart <= current.FeedbackDateTime && current.FeedbackDateTime <= reviewEnd;
                        if (!wasDuringReview)
                            continue;
                    }

                    var previous = i == 0 ? null : items[i - 1];

                    TimeSpan timeCode;

                    if (previous == null || video == null)
                    {
                        timeCode = TimeSpan.Zero;
                    }
                    else
                    {
                        timeCode = (previous.FeedbackDateTime - video.StartDateTime).Add(TimeSpan.FromSeconds(10));
                        var videoDuration = video.EndDateTime - video.StartDateTime;
                        if (timeCode >= videoDuration)
                            timeCode = result[i - 1].VideoTimeCode;
                    }


                    var feedbackWithVideo = new ApiReviewFeedbackWithVideo
                    {
                        Feedback = current,
                        Video = video,
                        VideoTimeCode = timeCode
                    };

                    result.Add(feedbackWithVideo);
                }

                return new ApiReviewSummary
                {
                    Video = video,
                    Items = result
                };
            }
        }
    }
}
