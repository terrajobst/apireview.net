using System.Security.Claims;

using ApiReviewDotNet;
using ApiReviewDotNet.Data;
using ApiReviewDotNet.Services;
using ApiReviewDotNet.Services.Calendar;
using ApiReviewDotNet.Services.GitHub;
using ApiReviewDotNet.Services.Ospo;
using ApiReviewDotNet.Services.YouTube;

using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new TimeSpanJsonConverter());
});
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddSingleton<IssueService>();
builder.Services.AddSingleton<GitHubClientFactory>();
builder.Services.AddSingleton<YouTubeServiceFactory>();
builder.Services.AddSingleton<AreaOwnerService>();
builder.Services.AddSingleton<OspoService>();
builder.Services.AddSingleton<RepositoryGroupService>();
builder.Services.AddSingleton<WebhookEventProcessor, GitHubEventProcessor>();
builder.Services.AddSingleton<CalendarService>();
builder.Services.AddSingleton<YouTubeManager>();
builder.Services.AddSingleton<GitHubMembershipService>();
builder.Services.AddSingleton<GitHubManager>();
builder.Services.AddSingleton<GitHubTeamService>();
builder.Services.AddSingleton<RefreshService>();

builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
    new DefaultAzureCredential());

builder.Services.AddSingleton<SummaryManager>();
builder.Services.AddSingleton<SummaryPublishingService>();

builder.Services.AddScoped<TimeZoneService>();
builder.Services.AddScoped<NotesService>();
builder.Services.AddAuthentication(options =>
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
    options.ClientId = builder.Configuration["GitHubClientId"]!;
    options.ClientSecret = builder.Configuration["GitHubClientSecret"]!;
    options.ClaimActions.MapJsonKey(ApiReviewConstants.GitHubAvatarUrl, ApiReviewConstants.GitHubAvatarUrl);
    options.Events.OnCreatingTicket = async context =>
    {
        var groupService = context.HttpContext.RequestServices.GetRequiredService<RepositoryGroupService>();
        var membershipService = context.HttpContext.RequestServices.GetRequiredService<GitHubMembershipService>();

        var accessToken = context.AccessToken;
        var orgName = ApiReviewConstants.ApiApproverOrgName;
        var teamSlugs = groupService.ApproverTeamSlugs;
        if (accessToken is not null && context.Identity?.Name is not null)
        {
            var userName = context.Identity.Name;
            var isMember = await membershipService.IsMemberOfAnyTeamAsync(accessToken, orgName, teamSlugs, userName);
            if (isMember)
                context.Identity.AddClaim(new Claim(context.Identity.RoleClaimType, ApiReviewConstants.ApiApproverRole));

            context.Identity.AddClaim(new Claim(ApiReviewConstants.TokenClaim, accessToken));
        }
    };
});
builder.Services.AddHttpClient();

var app = builder.Build();

// Warm up services
var refreshService = app.Services.GetRequiredService<RefreshService>();
await refreshService.StartAsync();

if (app.Environment.IsDevelopment())
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

app.MapBlazorHub();
app.MapDefaultControllerRoute();
app.MapGitHubWebhooks();
app.MapFallbackToPage("/_Host");

app.Run();
