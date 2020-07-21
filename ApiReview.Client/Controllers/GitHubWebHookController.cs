using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

using ApiReview.Client.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiReview.Client.Controllers
{
    [ApiController]
    [Route("github-webhook")]
    [AllowAnonymous]
    public sealed class GitHubWebHookController : Controller
    {
        private readonly IssueChangedNotificationService _notificationService;

        public GitHubWebHookController(IssueChangedNotificationService notificationService)
        {
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

            _notificationService.Notify();
            return Ok();
        }
    }
}
