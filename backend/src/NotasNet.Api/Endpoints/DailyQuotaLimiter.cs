namespace NotasNet.Api.Endpoints;

public class DailyQuotaLimiter(int maxPerDay)
{
    private readonly object _lock = new();
    private DateOnly _day = DateOnly.FromDateTime(DateTime.UtcNow);
    private int _count;

    public bool TryAcquire()
    {
        lock (_lock)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today != _day)
            {
                _day = today;
                _count = 0;
            }
            if (_count >= maxPerDay) return false;
            _count++;
            return true;
        }
    }
}
