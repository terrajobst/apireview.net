using System;

namespace ApiReview.Client.Services
{
    public sealed class IssueChangedNotificationService
    {
        public void Notify()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Changed;
    }
}
