using System.Security.Claims;

using ApiReviewDotNet.Data;
using ApiReviewDotNet.Services;
using ApiReviewDotNet.Services.GitHub;
using ApiReviewDotNet.Services.Ospo;
using ApiReviewDotNet.Services.YouTube;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApiReviewDotNet
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

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages().AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new TimeSpanJsonConverter());
            });
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
            services.AddServerSideBlazor();
            services.AddControllers();
            services.AddHostedService<AreaOwnerServiceWarmup>();
            services.AddHostedService<OspoServiceWarmup>();
            services.AddHostedService<IssueCacheWarmUp>();
            services.AddSingleton<IssueService>();
            services.AddSingleton<GitHubClientFactory>();
            services.AddSingleton<YouTubeServiceFactory>();
            services.AddSingleton<AreaOwnerService>();
            services.AddSingleton<OspoService>();
            services.AddSingleton<RepositoryGroupService>();
            services.Configure<MailOptions>(Configuration.GetSection("Mail"));

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

            services.AddScoped<NotesService>();
            services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    })
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/signin";
                        options.LogoutPath = "/signout";
                    })
                    .AddGitHub(options =>
                    {
                        options.ClientId = Configuration["GitHubClientId"];
                        options.ClientSecret = Configuration["GitHubClientSecret"];
                        options.ClaimActions.MapJsonKey(ApiReviewConstants.GitHubAvatarUrl, ApiReviewConstants.GitHubAvatarUrl);
                        options.Events.OnCreatingTicket = async context =>
                        {
                            var accessToken = context.AccessToken;
                            var orgName = ApiReviewConstants.ApiApproverOrgName;
                            var teamName = ApiReviewConstants.ApiApproverTeamSlug;
                            var userName = context.Identity.Name;
                            var isMember = await GitHubAuthHelpers.IsMemberOfTeamAsync(accessToken, orgName, teamName, userName);
                            if (isMember)
                                context.Identity.AddClaim(new Claim(context.Identity.RoleClaimType, ApiReviewConstants.ApiApproverRole));

                            context.Identity.AddClaim(new Claim(ApiReviewConstants.TokenClaim, accessToken));
                        };
                    });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHostRedirection("apireviews.azurewebsites.net", "apireview.net");

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapDefaultControllerRoute();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
