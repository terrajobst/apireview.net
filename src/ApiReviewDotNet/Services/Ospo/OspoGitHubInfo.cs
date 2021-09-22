namespace ApiReviewDotNet.Services.Ospo
{
    public sealed class OspoGitHubInfo
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public List<string> Organizations { get; set; }
    }
}
