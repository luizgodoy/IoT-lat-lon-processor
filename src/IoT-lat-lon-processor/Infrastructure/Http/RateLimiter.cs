namespace IoT_lat_lon_processor.Infrastructure.Http;

public interface IRateLimiter
{
    long PauseCount { get; }
    Task<IDisposable> AcquireAsync(CancellationToken ct = default);
    Task NotifyRateLimitAsync(TimeSpan? retryAfter, CancellationToken ct = default);
}

public sealed class RateLimiter : IRateLimiter
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly int _intervalMs;
    private readonly object _timeLock = new();
    private DateTime _nextAllowedRequestTimeUtc = DateTime.MinValue;
    private long _pauseCount;

    public long PauseCount => Interlocked.Read(ref _pauseCount);

    public RateLimiter(int intervalMs = 1100, int maxConcurrency = 1)
    {
        _intervalMs = Math.Max(0, intervalMs);
        _concurrencySemaphore = new SemaphoreSlim(Math.Max(1, maxConcurrency), Math.Max(1, maxConcurrency));
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken ct = default)
    {
        await _concurrencySemaphore.WaitAsync(ct);

        TimeSpan delayNeeded;
        lock (_timeLock)
        {
            var now = DateTime.UtcNow;
            if (now < _nextAllowedRequestTimeUtc)
            {
                delayNeeded = _nextAllowedRequestTimeUtc - now;
            }
            else
            {
                delayNeeded = TimeSpan.Zero;
            }

            _nextAllowedRequestTimeUtc = (now > _nextAllowedRequestTimeUtc ? now : _nextAllowedRequestTimeUtc)
                .AddMilliseconds(_intervalMs);
        }

        if (delayNeeded > TimeSpan.Zero)
        {
            Interlocked.Increment(ref _pauseCount);
            await Task.Delay(delayNeeded, ct);
        }

        return new Releaser(_concurrencySemaphore);
    }

    public async Task NotifyRateLimitAsync(TimeSpan? retryAfter, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _pauseCount);
        var delay = retryAfter ?? TimeSpan.FromSeconds(5);

        lock (_timeLock)
        {
            var targetTime = DateTime.UtcNow.Add(delay);
            if (targetTime > _nextAllowedRequestTimeUtc)
            {
                _nextAllowedRequestTimeUtc = targetTime;
            }
        }

        await Task.Delay(delay, ct);
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private int _disposed;

        public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _semaphore.Release();
            }
        }
    }
}
