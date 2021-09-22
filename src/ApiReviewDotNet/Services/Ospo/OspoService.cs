namespace ApiReviewDotNet.Services.Ospo;

public sealed class OspoService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public OspoService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task ReloadAsync()
    {
        if (_environment.IsDevelopment())
            return;

        var client = new OspoClient(_configuration["OspoToken"]);
        LinkSet = await client.GetAllAsync();
    }

    public OspoLinkSet LinkSet { get; private set; } = OspoLinkSet.Empty;
}
