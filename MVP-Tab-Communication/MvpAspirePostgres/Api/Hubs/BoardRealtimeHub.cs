using Microsoft.AspNetCore.SignalR;

namespace MvpAspirePostgres.Api.Hubs;

/// <summary>
/// Push channel from the board backend to all Blazor frontends.
/// Clients do not call hub methods; they only receive BoardChanged / DragEvent.
/// </summary>
public sealed class BoardRealtimeHub : Hub
{
    public const string BoardChanged = "BoardChanged";
    public const string DragEvent = "DragEvent";
}
