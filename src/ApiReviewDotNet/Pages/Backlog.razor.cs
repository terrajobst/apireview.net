using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ApiReviewDotNet.Data;
using ApiReviewDotNet.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;

namespace ApiReviewDotNet.Pages
{
    public sealed partial class Backlog : IDisposable
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        private IssueService IssueService { get; set; }

        private string _filter;
        private SortedDictionary<string, bool> _milestones;
        private readonly HashSet<ApiReviewIssue> _checkedIssues = new HashSet<ApiReviewIssue>();

        public string Filter
        {
            get => _filter;
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                    ChangeUrl();
                }
            }
        }

        public IReadOnlyList<ApiReviewIssue> Issues => IssueService.Issues;
        public IEnumerable<ApiReviewIssue> VisibleIssues => Issues.Where(IsVisible);
        public IEnumerable<ApiReviewIssue> SelectedIssues => VisibleIssues.Where(_checkedIssues.Contains);

        protected override void OnInitialized()
        {
            LoadData();

            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

            var queryParameters = QueryHelpers.ParseQuery(uri.Query);

            if (queryParameters.TryGetValue("q", out var q))
                _filter = q;
            
            if (queryParameters.TryGetValue("m", out var selectedMilestones))
            {
                foreach (var m in _milestones.Keys.ToArray())
                    _milestones[m] = false;

                foreach (var m in selectedMilestones)
                {
                    if (_milestones.ContainsKey(m))
                        _milestones[m] = true;
                }
            }

            IssueService.Changed += IssuesChanged;
        }

        public void Dispose()
        {
            IssueService.Changed -= IssuesChanged;
        }

        private async void ChangeUrl()
        {
            var query = "";

            if (!string.IsNullOrEmpty(Filter))
                query += $"?q={Uri.EscapeDataString(Filter)}";

            var selectedMilestones = _milestones.Where(m => m.Value).Select(kv => kv.Key);
            if (selectedMilestones.Count() != _milestones.Count)
            {
                foreach (var m in selectedMilestones)
                    query += $"&m={Uri.EscapeDataString(m)}";
            }

            var uri = new UriBuilder(NavigationManager.Uri)
            {
                Query = query
            }.ToString();

            await JSRuntime.InvokeVoidAsync("Blazor.navigateTo",
                                            uri.ToString(),
                                            /* forceLoad */ false,
                                            /* replace */ true);
        }

        private void LoadData()
        {
            _milestones = CreateMilestones(Issues, _milestones);
            _checkedIssues.Clear();
        }

        private async void IssuesChanged(object sender, EventArgs e)
        {
            await InvokeAsync(() =>
            {
                LoadData();
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
            {
                _milestones[milestone] = !isChecked;
                ChangeUrl();
            }
        }

        private void ToggleAllMilestones(bool value)
        {
            foreach (var m in _milestones.Keys.ToArray())
                _milestones[m] = value;

            ChangeUrl();
        }

        private async Task CopySelectedItems()
        {
            var text = GetMarkdown(useOfficeMentions: false);
            var html = Markdig.Markdown.ToHtml(GetMarkdown(useOfficeMentions: true));
            await JSRuntime.InvokeVoidAsync("clipboardCopy.copyText", text, html);
            _checkedIssues.Clear();
        }

        private string GetMarkdown(bool useOfficeMentions)
        {
            var sb = new StringBuilder();

            foreach (var issue in SelectedIssues)
            {
                sb.AppendLine($"* [{issue.IdFull}]({issue.Url}): {issue.Title}");

                if (issue.Reviewers.Any())
                {
                    sb.Append("    -");

                    foreach (var reviewer in issue.Reviewers)
                    {
                        if (!useOfficeMentions)
                        {
                            sb.Append($" [{reviewer.Name}](https://github.com/{reviewer.GitHubUserName})");
                        }
                        else
                        {
                            var guid = Guid.NewGuid().ToString("N").ToUpper();
                            var id = $"OWAAM{guid}Z";
                            sb.AppendLine($" <a id=\"{id}\" href=\"{reviewer.Email}\">@{reviewer.Name}</a>");
                        }
                    }

                    sb.AppendLine();
                }
            }

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
