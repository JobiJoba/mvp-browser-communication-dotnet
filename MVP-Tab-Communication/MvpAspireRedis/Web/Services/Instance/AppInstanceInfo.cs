using System.Text.RegularExpressions;

namespace MvpAspireRedis.Web.Services.Instance;

public sealed partial class AppInstanceInfo : IAppInstanceInfo
{
    public AppInstanceInfo(IConfiguration configuration)
    {
        ShortId = Guid.NewGuid().ToString("N")[..8];
        ReplicaIndex = ResolveReplicaIndex(configuration);
    }

    public string ShortId { get; }

    public string? ReplicaIndex { get; }

    private static string? ResolveReplicaIndex(IConfiguration configuration)
    {
        var explicitLabel = configuration["MVP_DEMO_REPLICA"];
        if (!string.IsNullOrWhiteSpace(explicitLabel))
        {
            return explicitLabel;
        }

        var otel = configuration["OTEL_RESOURCE_ATTRIBUTES"];
        if (!string.IsNullOrWhiteSpace(otel))
        {
            foreach (var part in otel.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("service.instance.id=", StringComparison.OrdinalIgnoreCase))
                {
                    return part["service.instance.id=".Length..];
                }
            }
        }

        var host = Environment.GetEnvironmentVariable("HOSTNAME")
            ?? Environment.GetEnvironmentVariable("COMPUTERNAME")
            ?? Environment.MachineName;

        var suffix = ReplicaSuffixPattern().Match(host);
        if (suffix.Success)
        {
            return suffix.Groups[1].Value;
        }

        return configuration["ASPIRE_REPLICA_INDEX"]
            ?? configuration["WEBSITE_INSTANCE_ID"];
    }

    [GeneratedRegex(@"-(\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex ReplicaSuffixPattern();
}
