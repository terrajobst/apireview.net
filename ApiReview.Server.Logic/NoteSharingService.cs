using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ApiReview.Shared;

using Octokit;

namespace ApiReview.Server.Logic
{
    public sealed class NoteSharingService
    {
        public async Task ShareNotesAsync(ApiReviewSummary summary)
        {
            await UpdateVideoDescriptionAsync(summary);
            await UpdateCommentsAsync(summary);
            await CommitAsync(summary);
            // await SendEmailAsync(summary);
        }

        //private static Task SendEmailAsync(ApiReviewSummary summary)
        //{
        //    var markdown = summary.GetMarkdown();
        //    var html = Markdown.ToHtml(markdown);

        //    var outlookApp = new Microsoft.Office.Interop.Outlook.Application();
        //    var mailItem = (MailItem)outlookApp.CreateItem(OlItemType.olMailItem);
        //    mailItem.To = "FXDR";
        //    mailItem.Subject = $"API Review Notes {Date.ToString("d")}";
        //    mailItem.HTMLBody = html;
        //    mailItem.Send();
        //}

        private async Task UpdateVideoDescriptionAsync(ApiReviewSummary summary)
        {
            if (summary.Video == null)
                return;

            using var descriptionBuilder = new StringWriter();
            foreach (var item in summary.Items)
            {
                var tc = item.VideoTimeCode;
                descriptionBuilder.WriteLine($"{tc.Hours:00}:{tc.Minutes:00}:{tc.Seconds:00} - {item.Feedback.Decision}: {item.Feedback.Issue.Title} {item.Feedback.FeedbackUrl}");
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

        private static async Task UpdateCommentsAsync(ApiReviewSummary summary)
        {
            var github = GitHubClientFactory.Create();

            foreach (var item in summary.Items)
            {
                var feedback = item.Feedback;

                if (feedback.VideoUrl == null && feedback.FeedbackId != null)
                {
                    var updatedMarkdown = $"[Video]({item.VideoTimeCodeUrl})\n\n{feedback.FeedbackMarkdown}";
                    var commentId = Convert.ToInt32(feedback.FeedbackId);
                    await github.Issue.Comment.Update(feedback.Issue.Owner, feedback.Issue.Repo, commentId, updatedMarkdown);
                }
            }
        }

        private static async Task CommitAsync(ApiReviewSummary summary)
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

        private static string GetMarkdown(ApiReviewSummary summary)
        {
            var noteWriter = new StringWriter();

            foreach (var item in summary.Items)
            {
                var feedback = item.Feedback;

                noteWriter.WriteLine($"## {feedback.Issue.Title}");
                noteWriter.WriteLine();
                noteWriter.Write($"**{feedback.Decision}** | [#{feedback.Issue.Repo}/{feedback.Issue.Id}]({feedback.FeedbackUrl})");

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
    }
}
