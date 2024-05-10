using System.Text.Json.Serialization;

namespace ApiReviewDotNet.Services.Ospo;

public sealed class OspoLink
{
    public OspoLink(OspoGitHubInfo gitHubInfo,
                    OspoMicrosoftInfo microsoftInfo)
    {
        GitHubInfo = gitHubInfo;
        MicrosoftInfo = microsoftInfo;
    }

    [JsonPropertyName("github")]
    public OspoGitHubInfo GitHubInfo { get; }

    [JsonPropertyName("aad")]
    public OspoMicrosoftInfo MicrosoftInfo { get; }
}
