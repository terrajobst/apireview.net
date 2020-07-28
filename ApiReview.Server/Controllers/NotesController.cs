using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using ApiReview.Shared;
using ApiReview.Server.Services;

namespace ApiReview.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = ApiReviewConstants.ApiApproverRole)]
    public class NotesController : ControllerBase
    {
        private readonly ILogger<NotesController> _logger;
        private readonly IYouTubeManager _youTubeManager;
        private readonly SummaryManager _summaryManager;
        private readonly SummaryPublishingService _summaryPublishingService;

        public NotesController(ILogger<NotesController> logger,
                               IYouTubeManager youTubeManager,
                               SummaryManager summaryManager,
                               SummaryPublishingService summaryPublishingService)
        {
            _logger = logger;
            _youTubeManager = youTubeManager;
            _summaryManager = summaryManager;
            _summaryPublishingService = summaryPublishingService;
        }

        [HttpGet("videos")]
        public Task<IReadOnlyList<ApiReviewVideo>> GetVideos(DateTimeOffset start, DateTimeOffset end)
        {
            return _youTubeManager.GetVideosAsync(start, end);
        }

        [HttpGet("video/{videoId}")]
        public Task<ApiReviewVideo> GetVideo(string videoId)
        {
            return _youTubeManager.GetVideoAsync(videoId);
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

        [HttpPost("publish")]
        public async Task<IActionResult> Publish([FromBody] ApiReviewSummary summary)
        {
            if (summary?.Items?.Any() != true)
                return Ok();

            try
            {
                var result = await _summaryPublishingService.PublishAsync(summary);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can't publish notes");
                return Problem();
            }
        }
    }
}
