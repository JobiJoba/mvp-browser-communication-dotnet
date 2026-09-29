namespace MvpAspireRedis.Web.Infrastructure;

internal static class RedisKeys
{
    public const string BoardItems = "mvp:board:items";
    public const string BoardChangedChannel = "mvp:board:changed";
    public const string DragEventsChannel = "mvp:drag:events";

    public static string DragSession(string sessionId) => $"mvp:drag:session:{sessionId}";
}
