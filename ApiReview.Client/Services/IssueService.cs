using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

using ApiReview.Shared;

namespace ApiReview.Client.Services
{
    public sealed class IssueService : IDisposable
    {
        private readonly BackendHttpClientFactory _clientFactory;
        private readonly IssueChangedNotificationService _notificationService;
        private IReadOnlyList<ApiReviewIssue> _issues;

        public IssueService(BackendHttpClientFactory clientFactory, IssueChangedNotificationService notificationService)
        {
            _clientFactory = clientFactory;
            _notificationService = notificationService;
            _notificationService.Changed += NotificationService_Changed;
        }

        public void Dispose()
        {
            _notificationService.Changed -= NotificationService_Changed;
        }

        private void NotificationService_Changed(object sender, EventArgs e)
        {
            _issues = null;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public async Task<IReadOnlyList<ApiReviewIssue>> GetAsync()
        {
            if (_issues == null)
            {
                var client = await _clientFactory.CreateAsync();
                _issues = await client.GetFromJsonAsync<IReadOnlyList<ApiReviewIssue>>("issues", _clientFactory.JsonOptions);
            }

            return _issues;
        }

        public event EventHandler Changed;
    }
}
