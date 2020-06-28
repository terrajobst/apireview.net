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
        [HttpGet("issues-for-range")]
        public Task<ApiReviewSummary> IssuesForRange(DateTimeOffset start, DateTimeOffset end, bool video)
        {
            return ApiReviewClient.GetSummaryAsync(start, end, video);
            // return ApiReviewClient.GetFakeSummaryAsync();
        }

        [HttpGet("issues-for-video")]
        public Task<ApiReviewSummary> IssuesForVideo(string videoId)
        {
            return ApiReviewClient.GetSummaryAsync(videoId);
        }

        [HttpGet("videos")]
        public Task<IReadOnlyList<ApiReviewVideo>> GetVideos(DateTimeOffset start, DateTimeOffset end)
        {
            return ApiReviewClient.GetVideosAsync(start, end);
            // return ApiReviewClient.GetFakeVideosAsync();
        }
    }
}
