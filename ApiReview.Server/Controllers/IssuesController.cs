using System.Collections.Generic;
using System.Threading.Tasks;

using ApiReview.Shared;
using ApiReview.Server.Logic;

using Microsoft.AspNetCore.Mvc;

namespace ApiReview.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IssuesController : ControllerBase
    {
        private readonly IGitHubManager _gitHubManager;

        public IssuesController(IGitHubManager gitHubManager)
        {
            _gitHubManager = gitHubManager;
        }

        [HttpGet]
        public Task<IReadOnlyList<ApiReviewIssue>> Get()
        {
            return _gitHubManager.GetIssuesAsync();
        }
    }
}
