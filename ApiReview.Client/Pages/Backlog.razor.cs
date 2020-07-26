using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using ApiReview.Client.Services;
using ApiReview.Shared;

namespace ApiReview.Client.Pages
{
    [Authorize]
    public partial class Backlog : IDisposable
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        private IssueService IssueService { get; set; }

        private IReadOnlyList<ApiReviewIssue> _issues;
        private SortedDictionary<string, bool> _milestones;
        private readonly HashSet<ApiReviewIssue> _checkedIssues = new HashSet<ApiReviewIssue>();

        public string Filter { get; set; }
        public IEnumerable<ApiReviewIssue> Issues => _issues ?? Array.Empty<ApiReviewIssue>();
        public IEnumerable<ApiReviewIssue> VisibleIssues => Issues.Where(IsVisible);
        public IEnumerable<ApiReviewIssue> SelectedIssues => VisibleIssues.Where(_checkedIssues.Contains);

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            IssueService.Changed += IssuesChanged;
        }

        public void Dispose()
        {
            IssueService.Changed -= IssuesChanged;
        }

        private async Task LoadData()
        {
            _issues = await IssueService.GetAsync();
            _milestones = CreateMilestones(_issues, _milestones);
            _checkedIssues.Clear();
        }

        private async void IssuesChanged(object sender, EventArgs e)
        {
            await InvokeAsync(async () =>
            {
                await LoadData();
                StateHasChanged();
            });
        }

        private bool IsVisible(ApiReviewIssue issue)
        {
            if (_milestones != null && _milestones.TryGetValue(issue.Milestone, out var isChecked) && !isChecked)
                return false;

            if (string.IsNullOrEmpty(Filter))
                return true;

            if (issue.Title.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                return true;

            if (issue.IdFull.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                return true;

            if (issue.Author.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var label in issue.Labels)
            {
                if (label.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private SortedDictionary<string, bool> CreateMilestones(IReadOnlyList<ApiReviewIssue> issues,
                                                                SortedDictionary<string, bool> existingMilestones)
        {
            var result = new SortedDictionary<string, bool>();

            foreach (var issue in issues)
                result[issue.Milestone] = true;

            if (existingMilestones != null)
            {
                foreach (var (k, v) in existingMilestones)
                {
                    if (result.ContainsKey(k))
                        result[k] = v;
                }
            }

            return result;
        }

        private void MilestoneChecked(string milestone)
        {
            if (_milestones.TryGetValue(milestone, out var isChecked))
                _milestones[milestone] = !isChecked;
        }

        private async Task CopySelectedItems()
        {
            var text = GetMarkdown();
            if (text == null)
                return;

            var html = Markdig.Markdown.ToHtml(text);
            await JSRuntime.InvokeVoidAsync("clipboardCopy.copyText", text, html);
            _checkedIssues.Clear();
        }

        private string GetMarkdown()
        {
            var sb = new System.Text.StringBuilder();

            foreach (var issue in SelectedIssues)
                sb.AppendLine($"* [{issue.IdFull}]({issue.Url}): {issue.Title}");

            return sb.ToString();
        }

        private void CheckAllIssues(bool value)
        {
            if (value)
                _checkedIssues.UnionWith(VisibleIssues);
            else
                _checkedIssues.ExceptWith(VisibleIssues);
        }

        private void CheckIssue(ApiReviewIssue issue, bool value)
        {
            if (value)
                _checkedIssues.Add(issue);
            else
                _checkedIssues.Remove(issue);
        }
    }
}
