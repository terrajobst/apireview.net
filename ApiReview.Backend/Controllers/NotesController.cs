using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ApiReview.Data;
using ApiReview.Logic;

using Microsoft.AspNetCore.Mvc;

namespace ApiReview.Backend.Controllers
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
        public Task<ApiReviewSummary> IssuesForRange(DateTimeOffset start, DateTimeOffset end, bool video)
        {
            return _summaryManager.GetSummaryAsync(start, end, video);
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
