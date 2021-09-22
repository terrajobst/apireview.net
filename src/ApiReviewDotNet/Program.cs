﻿using System.Security.Claims;

using ApiReviewDotNet;
using ApiReviewDotNet.Data;
using ApiReviewDotNet.Services;
using ApiReviewDotNet.Services.GitHub;
using ApiReviewDotNet.Services.Ospo;
using ApiReviewDotNet.Services.YouTube;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new TimeSpanJsonConverter());
});
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddHostedService<AreaOwnerServiceWarmup>();
builder.Services.AddHostedService<OspoServiceWarmup>();
builder.Services.AddSingleton<IssueService>();
builder.Services.AddSingleton<GitHubClientFactory>();
builder.Services.AddSingleton<YouTubeServiceFactory>();
builder.Services.AddSingleton<AreaOwnerService>();
builder.Services.AddSingleton<OspoService>();
builder.Services.AddSingleton<RepositoryGroupService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IYouTubeManager, FakeYouTubeManager>();
    builder.Services.AddSingleton<IGitHubManager, FakeGitHubManager>();
}
else
{
    builder.Services.AddSingleton<IYouTubeManager, YouTubeManager>();
    builder.Services.AddSingleton<IGitHubManager, GitHubManager>();
}

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
    options.ClientId = builder.Configuration["GitHubClientId"];
    options.ClientSecret = builder.Configuration["GitHubClientSecret"];
    options.ClaimActions.MapJsonKey(ApiReviewConstants.GitHubAvatarUrl, ApiReviewConstants.GitHubAvatarUrl);
    options.Events.OnCreatingTicket = async context =>
    {
        var groupService = context.HttpContext.RequestServices.GetRequiredService<RepositoryGroupService>();

        var accessToken = context.AccessToken;
        var orgName = ApiReviewConstants.ApiApproverOrgName;
        var teamSlugs = groupService.ApproverTeamSlugs;
        if (accessToken is not null && context.Identity?.Name is not null)
        {
            var userName = context.Identity.Name;
            var isMember = await GitHubAuthHelpers.IsMemberOfAnyTeamAsync(accessToken, orgName, teamSlugs, userName);
            if (isMember)
                context.Identity.AddClaim(new Claim(context.Identity.RoleClaimType, ApiReviewConstants.ApiApproverRole));

            context.Identity.AddClaim(new Claim(ApiReviewConstants.TokenClaim, accessToken));
        }
    };
});

var app = builder.Build();

// Warm up services
app.Services.GetRequiredService<IssueService>();

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
app.MapFallbackToPage("/_Host");

app.Run();
