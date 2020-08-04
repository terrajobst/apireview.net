using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace ApiReview.Client.Services
{
    public class BackendHttpClientFactory
    {
        private readonly string _url;

        public BackendHttpClientFactory(IConfiguration configuration,
                                        IOptions<JsonOptions> jsonOptions)
        {
            _url = configuration["apireview-api-url"];
            JsonOptions = jsonOptions.Value.JsonSerializerOptions;
        }

        public JsonSerializerOptions JsonOptions { get; }

        public virtual Task<HttpClient> CreateAsync()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(_url),
            };

            return Task.FromResult(client);
        }
    }
}
