using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using ApiReview.Server.Services;

namespace ApiReview.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class IssuesController : ControllerBase
    {
        private readonly ILogger<IssuesController> _logger;
        private readonly IGitHubManager _gitHubManager;

        public IssuesController(ILogger<IssuesController> logger, IGitHubManager gitHubManager)
        {
            _logger = logger;
            _gitHubManager = gitHubManager;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var result = await _gitHubManager.GetIssuesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can't retreive issues.");
                return Problem();
            }
        }
    }
}
