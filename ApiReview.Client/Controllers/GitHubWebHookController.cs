using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

using ApiReview.Client.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ApiReview.Client.Controllers
{
    [ApiController]
    [Route("github-webhook")]
    [AllowAnonymous]
    public sealed class GitHubWebHookController : Controller
    {
        private readonly ILogger<GitHubWebHookController> _logger;
        private readonly IssueChangedNotificationService _notificationService;

        public GitHubWebHookController(ILogger<GitHubWebHookController> logger,
                                       IssueChangedNotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            if (Request.ContentType != MediaTypeNames.Application.Json)
                return BadRequest();

            // Get payload
            string payload;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                payload = await reader.ReadToEndAsync();

            _logger.LogInformation("GitHub web hook {payload}", payload);
            _notificationService.Notify();
            return Ok();
        }
    }
}
