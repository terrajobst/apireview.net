using System;
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
        [HttpGet("{date}")]
        public Task<ApiReviewSummary> Get(DateTimeOffset date)
        {
            return ApiReviewClient.GetFakeSummaryAsync(date);
        }
    }
}
