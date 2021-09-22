namespace ApiReviewDotNet.Services.Ospo;

public sealed class OspoServiceWarmup : IHostedService, IDisposable
{
    private readonly OspoService _ospoService;
    private Timer? _timer;

    public OspoServiceWarmup(OspoService ospoService)
    {
        _ospoService = ospoService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _ospoService.ReloadAsync();
        var interval = TimeSpan.FromHours(1);
        _timer = new Timer(Refresh, null, interval, interval);
    }

    private async void Refresh(object? state)
    {
        await _ospoService.ReloadAsync();
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
