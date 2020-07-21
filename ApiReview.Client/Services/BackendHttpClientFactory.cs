using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.Authorization;

using ApiReview.Shared;

namespace ApiReview.Client.Services
{
    public sealed class BackendHttpClientFactory
    {
        private readonly string _url;
        private readonly AuthenticationStateProvider _provider;

        public BackendHttpClientFactory(IConfiguration configuration,
                                        AuthenticationStateProvider provider,
                                        IOptions<JsonOptions> jsonOptions)
        {
            _url = configuration["apireview-api-url"];
            JsonOptions = jsonOptions.Value.JsonSerializerOptions;
            _provider = provider;
        }

        public JsonSerializerOptions JsonOptions { get; }

        public async Task<HttpClient> CreateAsync()
        {
            var state = await _provider.GetAuthenticationStateAsync();
            var token = state.User.FindFirst(ApiReviewConstants.TokenClaim)?.Value;
            var client = new HttpClient
            {
                BaseAddress = new Uri(_url),
                DefaultRequestHeaders = {
                    { "Authorization", "Bearer " + token }
                }
            };
            return client;
        }
    }
}
