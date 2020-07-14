using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ApiReview.Shared;
using ApiReview.Server.Services;
using ApiReview.Server.GitHubAuth;

namespace ApiReview.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new TimeSpanJsonConverter());
            });
            services.AddSingleton<GitHubClientFactory>();
            services.AddSingleton<YouTubeServiceFactory>();

            if (Env.IsDevelopment())
            {
                services.AddSingleton<IYouTubeManager, FakeYouTubeManager>();
                services.AddSingleton<IGitHubManager, FakeGitHubManager>();
            }
            else
            {
                services.AddSingleton<IYouTubeManager, YouTubeManager>();
                services.AddSingleton<IGitHubManager, GitHubManager>();
            }

            services.AddSingleton<SummaryManager>();
            services.AddSingleton<SummaryPublishingService>();
            services.AddHttpContextAccessor();

            services.AddAuthentication("Bearer").AddGitHubBearer();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
