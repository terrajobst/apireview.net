using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ApiReview.Shared;
using ApiReview.Server.Logic;

using Microsoft.AspNetCore.Mvc;

namespace ApiReview.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly SummaryManager _summaryManager;
        private readonly IYouTubeManager _youTubeManager;

        public NotesController(SummaryManager summaryManager, IYouTubeManager youTubeManager)
        {
            _summaryManager = summaryManager;
            _youTubeManager = youTubeManager;
        }

        [HttpGet("issues-for-range")]
        public Task<ApiReviewSummary> IssuesForRange(DateTimeOffset start, DateTimeOffset end)
        {
            return _summaryManager.GetSummaryAsync(start, end);
        }

        [HttpGet("issues-for-video")]
        public Task<ApiReviewSummary> IssuesForVideo(string videoId)
        {
            return _summaryManager.GetSummaryAsync(videoId);
        }

        [HttpGet("videos")]
        public Task<IReadOnlyList<ApiReviewVideo>> GetVideos(DateTimeOffset start, DateTimeOffset end)
        {
            return _youTubeManager.GetVideosAsync(start, end);
        }
    }
}
