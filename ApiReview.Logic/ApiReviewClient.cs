using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ApiReview.Data;

using Google.Apis.YouTube.v3.Data;

using Octokit;

using static Google.Apis.YouTube.v3.SearchResource.ListRequest;

namespace ApiReview.Logic
{
    public static class ApiReviewClient
    {
        private const string _repoList = "dotnet/runtime,dotnet/winforms";
        private const string _netFoundationChannelId = "UCiaZbznpWV1o-KLxj8zqR6A";

        public static async Task<ApiReviewSummary> GetSummaryAsync(DateTimeOffset start, DateTimeOffset end, bool includeVideo)
        {
            var repos = OrgAndRepo.ParseList(_repoList).ToArray();
            var video = !includeVideo ? null : await GetVideoAsync(start, end);
            var items = await GetFeedbackAsync(repos, start, end);
            return CreateSummary(video, items);
        }

        public static async Task<ApiReviewSummary> GetSummaryAsync(string videoId)
        {
            var repos = OrgAndRepo.ParseList(_repoList).ToArray();
            var video = await GetVideoAsync(videoId);
            if (video == null)
                return null;

            var start = video.StartDateTime;
            var end = video.EndDateTime;
            var items = await GetFeedbackAsync(repos, start, end);
            return CreateSummary(video, items);
        }

        private static ApiReviewSummary CreateSummary(ApiReviewVideo video, IReadOnlyList<ApiReviewFeedback> items)
        {
            if (items.Count == 0)
            {
                return new ApiReviewSummary
                {
                    Video = video,
                    Items = Array.Empty<ApiReviewFeedbackWithVideo>()
                };
            }
            else
            {
                var result = new List<ApiReviewFeedbackWithVideo>();
                var reviewStart = video == null
                                    ? items.OrderBy(i => i.FeedbackDateTime).Select(i => i.FeedbackDateTime).First()
                                    : video.StartDateTime;

                var reviewEnd = video == null
                                    ? items.OrderBy(i => i.FeedbackDateTime).Select(i => i.FeedbackDateTime).Last()
                                    : video.EndDateTime.AddMinutes(15);

                for (var i = 0; i < items.Count; i++)
                {
                    var current = items[i];

                    if (video != null)
                    {
                        var wasDuringReview = reviewStart <= current.FeedbackDateTime && current.FeedbackDateTime <= reviewEnd;
                        if (!wasDuringReview)
                            continue;
                    }

                    var previous = i == 0 ? null : items[i - 1];

                    TimeSpan timeCode;

                    if (previous == null || video == null)
                    {
                        timeCode = TimeSpan.Zero;
                    }
                    else
                    {
                        timeCode = (previous.FeedbackDateTime - video.StartDateTime).Add(TimeSpan.FromSeconds(10));
                        var videoDuration = video.EndDateTime - video.StartDateTime;
                        if (timeCode >= videoDuration)
                            timeCode = result[i - 1].VideoTimeCode;
                    }


                    var feedbackWithVideo = new ApiReviewFeedbackWithVideo
                    {
                        Feedback = current,
                        Video = video,
                        VideoTimeCode = timeCode
                    };

                    result.Add(feedbackWithVideo);
                }

                return new ApiReviewSummary
                {
                    Video = video,
                    Items = result
                };
            }
        }

        public static Task<ApiReviewSummary> GetFakeSummaryAsync()
        {
            var video = new ApiReviewVideo(
                "t-X09mGPvNM",
                DateTimeOffset.Parse("2020-06-18T17:04:46Z"),
                DateTimeOffset.Parse("2020-06-18T19:11:47Z"),
                "GitHub Quick Reviews",
                "https://i.ytimg.com/vi/t-X09mGPvNM/default.jpg"
            );

            var apiApproved = new ApiReviewLabel
            {
                BackgroundColor = "159818",
                Name = "api-approved",
                Description = "API was approved in API review, it can be implemented"
            };

            var apiNeedsWork = new ApiReviewLabel
            {
                BackgroundColor = "207de5",
                Name = "api-needs-work",
                Description = "API needs work before it is approved, it is NOT ready for implementation"
            };

            var areaSystemNetSecurity = new ApiReviewLabel
            {
                BackgroundColor = "d4c5f9",
                Name = "area-System.Net.Security",
                Description = ""
            };

            var areaSystemComponentModelDataAnnotations = new ApiReviewLabel
            {
                BackgroundColor = "d4c5f9",
                Name = "area-System.ComponentModel.DataAnnotations",
                Description = ""
            };

            var areaSystemSecurity = new ApiReviewLabel
            {
                BackgroundColor = "d4c5f9",
                Name = "area-System.Security",
                Description = ""
            };

            var bug = new ApiReviewLabel
            {
                BackgroundColor = "f49cb1",
                Name = "bug",
                Description = "Product bug (most likely)"
            };

            var items = new[]
            {
                new ApiReviewFeedback()
                {
                    Issue = new ApiReviewIssue
                    {
                        Id = 37933,
                        Owner = "dotnet",
                        Repo = "runtime",
                        Title = "SslStream API improvements for enhanced use cases",
                        Author = "wfurt",
                        CreatedAt = DateTimeOffset.Parse("2020-06-15 03:20 AM -07:00"),
                        Labels = new[] { apiApproved, areaSystemNetSecurity },
                        Milestone = "",
                        Url = "https://github.com/dotnet/runtime/issues/37933",
                    },
                    FeedbackId = 646228075,
                    FeedbackUrl = "https://github.com/dotnet/runtime/issues/37933#issuecomment-646228075",
                    FeedbackStatus = "Approved",
                    FeedbackDateTime = DateTimeOffset.Parse("2020-06-18 11:17 AM -07:00"),
                    FeedbackMarkdown = @"* ServerOptionsSelectionCallback should use a context object instead of the hostname for future expansion
* Rename `sender` to `stream` in `ServerOptionsSelectionCallback`
* Why is `SslStream.HostName` `virtual`?
* Rename `HostName` to `TargetHostName`
* Rename `SslStreamCertificateContext.CreateForServer` to `SslStreamCertificateContext.Create` to unify with client usage (and drop the usage type check)
* Add an `offline` parameter to the SslStreamCertificateContext Create method
* Add a debugger proxy to show the target cert.

```C#
public readonly struct SslClientHelloInfo
{
    public string ServerName { get; }
    public SslProtocols SslProtocols { get; }
}

public delegate ValueTask<SslServerAuthenticationOptions> ServerOptionsSelectionCallback(SslStream stream, SslClientHelloInfo clientHelloInfo, object? state, CancellationToken cancellationToken);

partial class SslStream
{
    public string TargetHostName { get; }
    public Task AuthenticateAsServerAsync(
        ServerOptionsSelectionCallback optionCallback,
        object? state,
        CancellationToken cancellationToken = default);      
}

public sealed class SslStreamCertificateContext
{
    public static SslStreamCertificateContext Create(
        X509Certificate2 target,
        X509Certificate2Collection? additionalCertificates,
        bool offline = false);
}

partial class SslServerAuthenticationOptions
{
    SslStreamCertificateContext ServerCertificateContext  { get; set; };
}
```"
                },
                new ApiReviewFeedback()
                {
                    Issue = new ApiReviewIssue
                    {
                        Id = 29214,
                        Owner = "dotnet",
                        Repo = "runtime",
                        Title = "CompareAttribute.Validate method does not create a ValidationResult with MemberNames",
                        Author = "ChrisJWoodcock",
                        CreatedAt = DateTimeOffset.Parse("2020-06-15 03:20 AM -07:00"),
                        Labels = new[] { apiNeedsWork, areaSystemComponentModelDataAnnotations, bug },
                        Milestone = "",
                        Url = "https://github.com/dotnet/runtime/issues/29214",
                    },
                    FeedbackId = 0,
                    FeedbackStatus = "Needs Work",
                    FeedbackDateTime = DateTimeOffset.Parse("2020-06-18 11:26 AM -07:00"),
                    FeedbackMarkdown = ""
                },
                new ApiReviewFeedback()
                {
                    Issue = new ApiReviewIssue
                    {
                        Id = 31400,
                        Owner = "dotnet",
                        Repo = "runtime",
                        Title = "Add support for validating complex or collection properties using System.ComponentModel.DataAnnotations.Validator",
                        Author = "pranavkm",
                        CreatedAt = DateTimeOffset.Parse("2020-06-15 03:20 AM -07:00"),
                        Labels = new[] { apiNeedsWork, areaSystemComponentModelDataAnnotations },
                        Milestone = "",
                        Url = "https://github.com/dotnet/runtime/issues/31400",
                    },
                    FeedbackId = 646249648,
                    FeedbackUrl = "https://github.com/dotnet/runtime/issues/31400#issuecomment-646249648",
                    FeedbackStatus = "Needs Work",
                    FeedbackDateTime = DateTimeOffset.Parse("2020-06-18 12:02 PM -07:00"),
                    FeedbackMarkdown = @"* The new attribute seems fine, but be careful in how it gets consumed so as to not surprise users who use the objects in multiple contexts (Blazor, EF6, etc).
* It seems like some new data belongs on ValidationContext, instead of new overloads to (Try)ValidateObject, around memberName-path representations and other handling of [ValidateComplexType]."
                },
                new ApiReviewFeedback()
                {
                    Issue = new ApiReviewIssue
                    {
                        Id = 31944,
                        Owner = "dotnet",
                        Repo = "runtime",
                        Title = "Add easy way to create a certificate from a multi-PEM or cert-PEM + key-PEM",
                        Author = "bartonjs",
                        CreatedAt = DateTimeOffset.Parse("2020-06-15 03:20 AM -07:00"),
                        Labels = new[] { apiApproved, areaSystemSecurity },
                        Milestone = "",
                        Url = "https://github.com/dotnet/runtime/issues/31944",
                    },
                    FeedbackId = 646253846,
                    FeedbackUrl = "https://github.com/dotnet/runtime/issues/31944#issuecomment-646253846",
                    FeedbackStatus = "Approved",
                    FeedbackDateTime = DateTimeOffset.Parse("2020-06-18 12:11 PM -07:00"),
                    FeedbackMarkdown = @"Approved without the byte-based password inputs.

```C#
namespace System.Security.Cryptography.X509Certificates
    {
        partial class X509Certificate2
        {
            public static X509Certificate2 CreateFromPemFile(string certPemFilePath, string keyPemFilePath = default);
            public static X509Certificate2 CreateFromEncryptedPemFile(string certPemFilePath, ReadOnlySpan<char> password, string keyPemFilePath = default);

            public static X509Certificate2 CreateFromPem(ReadOnlySpan<char> certPem, ReadOnlySpan<char> keyPem);
            public static X509Certificate2 CreateFromEncryptedPem(ReadOnlySpan<char> certPem, ReadOnlySpan<char> keyPem, ReadOnlySpan<char> password);
        }

        partial class X509Certificate2Collection
        {
            public void ImportFromPemFile(string certPemFilePath);
            public void ImportFromPem(ReadOnlySpan<char> certPem);
        }
    }
```"
                }
            };

            var result = CreateSummary(video, items);
            return Task.FromResult(result);
        }

        public static Task<IReadOnlyList<ApiReviewVideo>> GetFakeVideosAsync()
        {
            var result = new[]
            {
                new ApiReviewVideo(
                    "rx_098IdZU0",
                    DateTimeOffset.Parse("2020-06-25T16:54:00Z"),
                    DateTimeOffset.Parse("2020-06-25T19:02:22Z"),
                    "GitHub Quick Reviews",
                    "https://i.ytimg.com/vi/rx_098IdZU0/mqdefault.jpg"
                ),
                new ApiReviewVideo(
                    "q7ODj3RJnME",
                    DateTimeOffset.Parse("2020-06-25T16:53:18Z"),
                    DateTimeOffset.Parse("2020-06-25T17:42:57Z"),
                    "Desktop: .NET Community Standup - June 25th 2020 - New XAML Desktop Features",
                    "https://i.ytimg.com/vi/q7ODj3RJnME/mqdefault.jpg"
                )
            };

            return Task.FromResult<IReadOnlyList<ApiReviewVideo>>(result);
        }

        public static async Task<ApiReviewVideo> GetVideoAsync(DateTimeOffset publishedAfter, DateTimeOffset publishedBefore)
        {
            var videos = await GetVideosAsync(publishedAfter, publishedBefore);
            return videos.FirstOrDefault();
        }

        public static async Task<IReadOnlyList<ApiReviewVideo>> GetVideosAsync(DateTimeOffset publishedAfter, DateTimeOffset publishedBefore)
        {
            var service = await YouTubeServiceFactory.CreateAsync();

            var result = new List<Video>();
            var nextPageToken = "";

            var searchRequest = service.Search.List("snippet");
            searchRequest.ChannelId = _netFoundationChannelId;
            searchRequest.Type = "video";
            searchRequest.EventType = EventTypeEnum.Completed;
            searchRequest.PublishedAfter = publishedAfter.DateTime;
            searchRequest.PublishedBefore = publishedBefore.DateTime;
            searchRequest.MaxResults = 25;

            while (nextPageToken != null)
            {
                searchRequest.PageToken = nextPageToken;
                var response = await searchRequest.ExecuteAsync();

                var ids = response.Items.Select(i => i.Id.VideoId);
                var idString = string.Join(",", ids);

                var videoRequest = service.Videos.List("snippet,liveStreamingDetails");
                videoRequest.Id = idString;
                var videoResponse = await videoRequest.ExecuteAsync();
                result.AddRange(videoResponse.Items);

                nextPageToken = response.NextPageToken;
            }

            var videos = result.Where(v => v.LiveStreamingDetails != null &&
                                           v.LiveStreamingDetails.ActualStartTime != null &&
                                           v.LiveStreamingDetails.ActualEndTime != null)
                               .Select(CreateVideo)
                               .OrderByDescending(v => v.Duration);
            return videos.ToArray();
        }

        private static ApiReviewVideo CreateVideo(Video v)
        {
            return new ApiReviewVideo(v.Id,
                                      v.LiveStreamingDetails.ActualStartTime.Value,
                                      v.LiveStreamingDetails.ActualEndTime.Value,
                                      v.Snippet.Title,
                                      v.Snippet.Thumbnails?.Default__?.Url);
        }

        public static async Task<ApiReviewVideo> GetVideoAsync(string videoId)
        {
            var service = await YouTubeServiceFactory.CreateAsync();

            var videoRequest = service.Videos.List("snippet,liveStreamingDetails");
            videoRequest.Id = videoId;
            var videoResponse = await videoRequest.ExecuteAsync();
            if (videoResponse.Items.Count == 0)
                return null;

            return CreateVideo(videoResponse.Items[0]);
        }

        private static async Task<IReadOnlyList<ApiReviewFeedback>> GetFeedbackAsync(OrgAndRepo[] repos, DateTimeOffset start, DateTimeOffset end)
        {
            static string GetApiStatus(Issue issue)
            {
                var isReadyForReview = issue.Labels.Any(l => l.Name == "api-ready-for-review");
                var isApproved = issue.Labels.Any(l => l.Name == "api-approved");
                var needsWork = issue.Labels.Any(l => l.Name == "api-needs-work");
                var isRejected = isReadyForReview && issue.State.Value == ItemState.Closed;

                var isApi = isApproved || needsWork || isRejected;

                if (!isApi)
                    return null;

                if (isApproved)
                    return "Approved";

                if (isRejected)
                    return "Rejected";

                return "Needs Work";
            }

            static bool WasEverReadyForReview(Issue issue, IEnumerable<EventInfo> events)
            {
                if (issue.Labels.Any(l => l.Name == "api-ready-for-review" ||
                                          l.Name == "api-approved"))
                    return true;

                foreach (var eventInfo in events)
                {
                    if (eventInfo.Label?.Name == "api-ready-for-review" ||
                        eventInfo.Label?.Name == "api-approved")
                        return true;
                }

                return false;
            }

            static bool IsApiEvent(EventInfo eventInfo)
            {
                // We need to work around unsupported enum values:
                // - https://github.com/octokit/octokit.net/issues/2023
                // - https://github.com/octokit/octokit.net/issues/2025
                //
                // which will cause Value to throw an exception.

                switch (eventInfo.Event.StringValue)
                {
                    case "labeled":
                        if (eventInfo.Label.Name == "api-approved" || eventInfo.Label.Name == "api-needs-work")
                            return true;
                        break;
                    case "closed":
                        return true;
                }

                return false;
            }

            static IEnumerable<EventInfo> GetApiEvents(IEnumerable<EventInfo> events, DateTimeOffset start, DateTimeOffset end)
            {
                foreach (var eventGroup in events.Where(e => start <= e.CreatedAt && e.CreatedAt <= end && IsApiEvent(e))
                                                 .GroupBy(e => e.CreatedAt.Date))
                {
                    var latest = eventGroup.OrderBy(e => e.CreatedAt).Last();
                    yield return latest;
                }
            }

            static (string VideoLink, string Markdown) ParseFeedback(string body)
            {
                if (body == null)
                    return (null, null);

                const string prefix = "[Video](";
                if (body.StartsWith(prefix))
                {
                    var videoUrlEnd = body.IndexOf(")");
                    if (videoUrlEnd > 0)
                    {
                        var videoUrlStart = prefix.Length;
                        var videoUrlLength = videoUrlEnd - videoUrlStart;
                        var videoUrl = body.Substring(videoUrlStart, videoUrlLength);
                        var remainingBody = body.Substring(videoUrlEnd + 1).TrimStart();
                        return (videoUrl, remainingBody);
                    }
                }

                return (null, body);
            }

            var github = GitHubClientFactory.Create();
            var results = new List<ApiReviewFeedback>();

            foreach (var (owner, repo) in repos)
            {
                var request = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = ItemStateFilter.All,
                    Since = start
                };

                var issues = await github.Issue.GetAllForRepository(owner, repo, request);

                foreach (var issue in issues)
                {
                    var status = GetApiStatus(issue);
                    if (status == null)
                        continue;

                    var events = await github.Issue.Events.GetAllForIssue(owner, repo, issue.Number);

                    if (!WasEverReadyForReview(issue, events))
                        continue;

                    foreach (var apiEvent in GetApiEvents(events, start, end))
                    {
                        var title = GitHubIssueHelpers.FixTitle(issue.Title);
                        var feedbackDateTime = apiEvent.CreatedAt;
                        var comments = await github.Issue.Comment.GetAllForIssue(owner, repo, issue.Number);
                        var eventComment = comments.Where(c => c.User.Login == apiEvent.Actor.Login)
                                                   .Select(c => (comment: c, within: Math.Abs((c.CreatedAt - feedbackDateTime).TotalSeconds)))
                                                   .Where(c => c.within <= TimeSpan.FromMinutes(15).TotalSeconds)
                                                   .OrderBy(c => c.within)
                                                   .Select(c => c.comment)
                                                   .FirstOrDefault();
                        var feedbackId = eventComment?.Id;
                        var feedbackUrl = eventComment?.HtmlUrl ?? issue.HtmlUrl;
                        var (videoUrl, feedbackMarkdown) = ParseFeedback(eventComment?.Body);

                        var apiReviewIssue = CreateIssue(owner, repo, issue);

                        var feedback = new ApiReviewFeedback
                        {
                            Issue = apiReviewIssue,
                            FeedbackId = feedbackId,
                            FeedbackDateTime = feedbackDateTime,
                            FeedbackUrl = feedbackUrl,
                            FeedbackStatus = status,
                            FeedbackMarkdown = feedbackMarkdown,
                            VideoUrl = videoUrl
                        };
                        results.Add(feedback);
                    }
                }
            }

            results.Sort((x, y) => x.FeedbackDateTime.CompareTo(y.FeedbackDateTime));
            return results;
        }

        private static string GetMarkdown(ApiReviewSummary summary)
        {
            var noteWriter = new StringWriter();

            foreach (var item in summary.Items)
            {
                var feedback = item.Feedback;

                noteWriter.WriteLine($"## {feedback.Issue.Title}");
                noteWriter.WriteLine();
                noteWriter.Write($"**{feedback.FeedbackStatus}** | [#{feedback.Issue.Repo}/{feedback.Issue.Id}]({feedback.FeedbackUrl})");

                if (item.VideoTimeCodeUrl != null)
                    noteWriter.Write($" | [Video]({item.VideoTimeCodeUrl})");

                noteWriter.WriteLine();
                noteWriter.WriteLine();

                if (feedback.FeedbackMarkdown != null)
                {
                    noteWriter.Write(feedback.FeedbackMarkdown);
                    noteWriter.WriteLine();
                }
            }

            return noteWriter.ToString();
        }

        //public static void SendEmail(ApiReviewSummary summary)
        //{
        //    var markdown = summary.GetMarkdown();
        //    var html = Markdown.ToHtml(markdown);
        //
        //    var outlookApp = new Microsoft.Office.Interop.Outlook.Application();
        //    var mailItem = (MailItem)outlookApp.CreateItem(OlItemType.olMailItem);
        //    mailItem.To = "FXDR";
        //    mailItem.Subject = $"API Review Notes {Date.ToString("d")}";
        //    mailItem.HTMLBody = html;
        //    mailItem.Send();
        //}

        public static async Task UpdateVideoDescriptionAsync(ApiReviewSummary summary)
        {
            if (summary.Video == null)
                return;

            using var descriptionBuilder = new StringWriter();
            foreach (var item in summary.Items)
            {
                var tc = item.VideoTimeCode;
                descriptionBuilder.WriteLine($"{tc.Hours:00}:{tc.Minutes:00}:{tc.Seconds:00} - {item.Feedback.FeedbackStatus}: {item.Feedback.Issue.Title} {item.Feedback.FeedbackUrl}");
            }

            var description = descriptionBuilder.ToString()
                                                .Replace("<", "(")
                                                .Replace(">", ")");

            var service = await YouTubeServiceFactory.CreateAsync();

            var listRequest = service.Videos.List("snippet");
            listRequest.Id = summary.Video.Id;
            var listResponse = await listRequest.ExecuteAsync();

            var video = listResponse.Items[0];
            video.Snippet.Description = description;

            var updateRequest = service.Videos.Update(video, "snippet");
            await updateRequest.ExecuteAsync();
        }

        public static async Task UpdateCommentsAsync(ApiReviewSummary summary)
        {
            var github = GitHubClientFactory.Create();

            foreach (var item in summary.Items)
            {
                var feedback = item.Feedback;

                if (feedback.VideoUrl == null && feedback.FeedbackId != null)
                {
                    var updatedMarkdown = $"[Video]({item.VideoTimeCodeUrl})\n\n{feedback.FeedbackMarkdown}";
                    await github.Issue.Comment.Update(feedback.Issue.Owner, feedback.Issue.Repo, feedback.FeedbackId.Value, updatedMarkdown);
                }
            }
        }

        public static async Task CommitAsync(ApiReviewSummary summary)
        {
            if (summary.Items.Count == 0)
                return;

            var owner = "dotnet";
            var repo = "apireviews";
            var branch = "heads/master";
            var date = summary.Items.FirstOrDefault().Feedback.FeedbackDateTime.DateTime;
            var markdown = $"# Quick Reviews {date:d}\n\n{GetMarkdown(summary)}";
            var path = $"{date.Year}/{date.Month:00}-{date.Day:00}-quick-reviews/README.md";
            var commitMessage = $"Add quick review notes for {date:d}";

            var github = GitHubClientFactory.Create();
            var masterReference = await github.Git.Reference.Get(owner, repo, branch);
            var latestCommit = await github.Git.Commit.Get(owner, repo, masterReference.Object.Sha);

            var recursiveTreeResponse = await github.Git.Tree.GetRecursive(owner, repo, latestCommit.Tree.Sha);
            var file = recursiveTreeResponse.Tree.SingleOrDefault(t => t.Path == path);

            if (file == null)
            {
                var newTreeItem = new NewTreeItem
                {
                    Mode = "100644",
                    Path = path,
                    Content = markdown
                };

                var newTree = new NewTree
                {
                    BaseTree = latestCommit.Tree.Sha
                };
                newTree.Tree.Add(newTreeItem);

                var newTreeResponse = await github.Git.Tree.Create(owner, repo, newTree);
                var newCommit = new NewCommit(commitMessage, newTreeResponse.Sha, latestCommit.Sha);
                var newCommitResponse = await github.Git.Commit.Create(owner, repo, newCommit);

                var newReference = new ReferenceUpdate(newCommitResponse.Sha);
                var newReferenceResponse = await github.Git.Reference.Update(owner, repo, branch, newReference);
            }
        }

        public static async Task<IReadOnlyList<ApiReviewIssue>> GetIssues()
        {
            var repos = OrgAndRepo.ParseList(_repoList).ToArray();

            var github = GitHubClientFactory.Create();
            var result = new List<ApiReviewIssue>();

            foreach (var (owner, repo) in repos)
            {
                var request = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = ItemStateFilter.Open
                };
                request.Labels.Add("api-ready-for-review");

                var issues = await github.Issue.GetAllForRepository(owner, repo, request);

                foreach (var issue in issues)
                {
                    var apiReviewIssue = CreateIssue(owner, repo, issue);
                    result.Add(apiReviewIssue);
                }
            }

            result.Sort();

            return result;
        }

        private static ApiReviewIssue CreateIssue(string owner, string repo, Issue issue)
        {
            var result = new ApiReviewIssue
            {
                Owner = owner,
                Repo = repo,
                Author = issue.User.Login,
                CreatedAt = issue.CreatedAt,
                Labels = issue.Labels.Select(l => new ApiReviewLabel { Name = l.Name, BackgroundColor = l.Color, Description = l.Description }).ToArray(),
                Milestone = issue.Milestone?.Title ?? "(None)",
                Title = GitHubIssueHelpers.FixTitle(issue.Title),
                Url = issue.HtmlUrl,
                Id = issue.Number
            };
            return result;
        }
    }
}
