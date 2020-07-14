using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace ApiReview.Client.Services
{
    internal sealed class ApiReviewHttpClientFactory
    {
        private readonly string _url;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiReviewHttpClientFactory(IConfiguration configuration,
                                          IHttpContextAccessor httpContextAccessor,
                                          IOptions<JsonOptions> jsonOptions)
        {
            _url = configuration["apireview-api-url"];
            _httpContextAccessor = httpContextAccessor;
            JsonOptions = jsonOptions.Value.JsonSerializerOptions;
        }

        public JsonSerializerOptions JsonOptions { get; }

        public async Task<HttpClient> CreateAsync()
        {
            var token = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");

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
