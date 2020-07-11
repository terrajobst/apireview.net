using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace ApiReview.Client.Data
{
    internal sealed class ApiReviewHttpClientFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiReviewHttpClientFactory(IOptions<JsonOptions> jsonOptions, IHttpContextAccessor httpContextAccessor)
        {
            JsonOptions = jsonOptions.Value.JsonSerializerOptions;
            _httpContextAccessor = httpContextAccessor;
        }

        public JsonSerializerOptions JsonOptions { get; }

        public async Task<HttpClient> CreateAsync()
        {
            var token = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");

            var client = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:44305"),
                DefaultRequestHeaders = {
                    { "Authorization", "Bearer " + token }
                }
            };
            return client;
        }
    }
}
