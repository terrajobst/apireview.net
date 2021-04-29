using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace ApiReviewDotNet.Services
{
    internal sealed class IssueCacheWarmUp : IHostedService
    {
        private readonly IssueService _issueService;

        public IssueCacheWarmUp(IssueService issueService)
        {
            _issueService = issueService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _issueService.ReloadAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
