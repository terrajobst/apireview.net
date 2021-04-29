using System.Text.Json.Serialization;

namespace ApiReviewDotNet.Services.Ospo
{
    public sealed class OspoLink
    {
        [JsonPropertyName("github")]
        public OspoGitHubInfo GitHubInfo { get; set; }

        [JsonPropertyName("aad")]
        public OspoMicrosoftInfo MicrosoftInfo { get; set; }
    }
}
