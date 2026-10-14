namespace NotasNet.Api.Application;

public interface IUsageQuota
{
    bool TryAcquire();
}
