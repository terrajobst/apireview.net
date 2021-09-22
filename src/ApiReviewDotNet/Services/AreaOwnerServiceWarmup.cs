namespace ApiReviewDotNet.Services;

public sealed class AreaOwnerServiceWarmup : IHostedService, IDisposable
{
    private readonly AreaOwnerService _ownerService;
    private Timer? _timer;

    public AreaOwnerServiceWarmup(AreaOwnerService ospoService)
    {
        _ownerService = ospoService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _ownerService.ReloadAsync();
        var interval = TimeSpan.FromHours(1);
        _timer = new Timer(Refresh, null, interval, interval);
    }

    private async void Refresh(object? state)
    {
        await _ownerService.ReloadAsync();
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
