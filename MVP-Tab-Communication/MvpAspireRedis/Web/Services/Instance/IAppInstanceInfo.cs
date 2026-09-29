namespace MvpAspireRedis.Web.Services.Instance;

/// <summary>
/// Identifies which web replica is handling this Blazor circuit (for multi-container demos).
/// </summary>
public interface IAppInstanceInfo
{
    string ShortId { get; }

    string? ReplicaIndex { get; }
}
