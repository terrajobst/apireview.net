﻿using System.Text.Json;

using ApiReviewDotNet.Data;

using Octokit;

namespace RepoIndexer;

internal sealed class GitHubIssueCache
{
    private readonly string _path;
    private readonly string _gitHubKey;
    private Dictionary<(string Owner, string Repo, int Id), ApiReviewIssue>? _cache;
    private GitHubClient? _gitHubClient;

    public GitHubIssueCache(string path, string gitHubKey)
    {
        _path = path;
        _gitHubKey = gitHubKey;
    }

    public async Task<ApiReviewIssue> GetIssue(string owner, string repo, int number)
    {
        if (_cache == null)
            _cache = await LoadCache(_path);

        var key = (owner, repo, number);

        if (!_cache.TryGetValue(key, out var issue))
        {
            if (_gitHubClient == null)
            {
                var productInformation = new ProductHeaderValue("RepoIndexer");
                _gitHubClient = new GitHubClient(productInformation)
                {
                    Credentials = new Credentials(_gitHubKey)
                };
            }

            Console.WriteLine($"Loading issue {key.owner}/{key.repo}#{key.number}");

            var githubIssue = await _gitHubClient.Issue.Get(owner, repo, number);
            issue = CreateIssue(owner, repo, number, githubIssue);

            _cache.Add(key, issue);

            var issues = _cache.Values.OrderBy(r => r.Owner)
                                      .ThenBy(r => r.Repo)
                                      .ThenBy(r => r.Id);

            using (var stream = File.Create(_path))
                await JsonSerializer.SerializeAsync(stream, issues, MyJsonSerializerOptions.Instance);
        }

        return issue;
    }

    private static async Task<Dictionary<(string Owner, string Repo, int Id), ApiReviewIssue>> LoadCache(string path)
    {
        var cache = new Dictionary<(string Owner, string Repo, int Id), ApiReviewIssue>();

        if (File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            var issues = await JsonSerializer.DeserializeAsync<IReadOnlyList<ApiReviewIssue>>(stream) ?? Array.Empty<ApiReviewIssue>();

            foreach (var issue in issues)
            {
                var key = (issue.Owner, issue.Repo, issue.Id);
                cache.Add(key, issue);
            }
        }

        return cache;
    }

    private static ApiReviewIssue CreateIssue(string owner, string repo, int number, Issue issue)
    {
        var labels = issue.Labels.Select(l => new ApiReviewLabel(l.Name, l.Color, l.Description)).ToArray();

        var result = new ApiReviewIssue
        (
            owner: owner,
            repo: repo,
            id: number,
            author: issue.User.Login,
            assignees: issue.Assignees.Select(a => a.Login).ToArray(),
            createdAt: issue.CreatedAt,
            labels: labels,
            milestone: issue.Milestone?.Title ?? ApiReviewConstants.NoMilestone,
            title: GitHubIssueHelpers.FixTitle(issue.Title),
            url: issue.HtmlUrl,
            markedReadyForReviewBy: null,
            areaOwners: Array.Empty<string>(),
            reviewers: Array.Empty<ApiReviewer>()
        );

        return result;
    }
}