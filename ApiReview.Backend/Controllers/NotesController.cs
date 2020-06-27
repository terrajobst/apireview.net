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
        [HttpGet("date/{date}")]
        public Task<ApiReviewSummary> GetByDate(DateTimeOffset date)
        {
            return ApiReviewClient.GetSummaryAsync(date);
        }

        [HttpGet("video/{videoId}")]
        public Task<ApiReviewSummary> GetByVideo(string videoId)
        {
            return ApiReviewClient.GetSummaryAsync(videoId);
        }

        [HttpGet("videos/{date}")]
        public Task<IReadOnlyList<ApiReviewVideo>> GetVideos(DateTimeOffset date)
        {
            //return ApiReviewClient.GetVideosAsync(date);
            return ApiReviewClient.GetFakeVideosAsync();
        }
    }
}
