using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ApiReview.Server.Services
{
    public sealed class OspoService
    {
        private readonly IConfiguration _configuration;
        private OspoLinkSet _linkSet;

        public OspoService(IConfiguration configuration)
        {
           _configuration = configuration;
        }

        public async Task<OspoLinkSet> GetLinkSetAsync()
        {
            if (_linkSet == null)
            {
                await ReloadAsync();
            }

            return _linkSet;
        }

        public async Task ReloadAsync()
        {
            var client = new OspoClient(_configuration["OspoToken"]);
            _linkSet = await client.GetAllAsync();
        }
    }

    public sealed class OspoServiceWarmup : IHostedService, IDisposable
    {
        private readonly OspoService _ospoService;
        private Timer _timer;

        public OspoServiceWarmup(OspoService ospoService)
        {
            _ospoService = ospoService;            
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _ospoService.ReloadAsync();
            _timer = new Timer(Refresh, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        private async void Refresh(object state)
        {
            await _ospoService.ReloadAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }

    public sealed class OspoClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        public OspoClient(string token)
        {
            if (token is null)
                throw new ArgumentNullException(nameof(token));

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://repos.opensource.microsoft.com/api/")
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("api-version", "2019-10-01");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{token}")));
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task<OspoLinkSet> GetAllAsync()
        {
            var links = await GetAsJsonAsync<IReadOnlyList<OspoLink>>($"people/links");

            var linkSet = new OspoLinkSet
            {
                Links = links ?? Array.Empty<OspoLink>()
            };

            linkSet.Initialize();
            return linkSet;
        }

        private async Task<T> GetAsJsonAsync<T>(string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return default;

            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                throw new Exception(message) { HResult = (int)response.StatusCode };
            }

            var responseStream = await response.Content.ReadAsStreamAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return await JsonSerializer.DeserializeAsync<T>(responseStream, options);
        }
    }

    public sealed class OspoLink
    {
        [JsonPropertyName("github")]
        public OspoGitHubInfo GitHubInfo { get; set; }

        [JsonPropertyName("aad")]
        public OspoMicrosoftInfo MicrosoftInfo { get; set; }
    }

    public sealed class OspoGitHubInfo
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public List<string> Organizations { get; set; }
    }

    public sealed class OspoMicrosoftInfo
    {
        public string Alias { get; set; }
        public string PreferredName { get; set; }
        public string UserPrincipalName { get; set; }
        public string EmailAddress { get; set; }
        public string Id { get; set; }
    }

    public sealed class OspoLinkSet
    {
        public OspoLinkSet()
        {
        }

        public void Initialize()
        {
            LinkByLogin = Links.ToDictionary(l => l.GitHubInfo.Login);
        }

        public IReadOnlyList<OspoLink> Links { get; set; } = new List<OspoLink>();

        [JsonIgnore]
        public IReadOnlyDictionary<string, OspoLink> LinkByLogin { get; set; } = new Dictionary<string, OspoLink>();
    }
}
