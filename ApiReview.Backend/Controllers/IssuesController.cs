using System.Collections.Generic;
using System.Threading.Tasks;

using ApiReview.Data;
using ApiReview.Logic;

using Microsoft.AspNetCore.Mvc;

namespace ApiReview.Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IssuesController : ControllerBase
    {
        [HttpGet]
        public Task<IReadOnlyList<ApiReviewIssue>> Get()
        {
            return ApiReviewClient.GetIssues();
        }
    }
}
