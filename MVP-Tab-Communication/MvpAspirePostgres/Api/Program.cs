using MvpAspirePostgres.Api.Hubs;
using MvpAspirePostgres.Api.Models;
using MvpAspirePostgres.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("mvpdb");

builder.Services.AddSignalR();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<DragSessionService>();
builder.Services.AddHostedService<DragSessionSweeper>();

var app = builder.Build();

app.MapDefaultEndpoints();

var knownZones = new HashSet<string>(StringComparer.Ordinal) { "tray", "inbox", "archive" };

// Seed schema early so the first board GET is fast; observe failures.
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
_ = app.Services.GetRequiredService<BoardService>()
    .EnsureReadyAsync()
    .ContinueWith(
        t =>
        {
            if (t.IsFaulted)
            {
                logger.LogError(t.Exception!.GetBaseException(), "Board schema warm-up failed.");
            }
        },
        TaskScheduler.Default);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGet("/api/board", async (BoardService board, CancellationToken ct) =>
{
    var items = await board.GetItemsAsync(ct);
    return Results.Ok(items);
});

app.MapPost("/api/board/move", async (MoveItemRequest request, BoardService board, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.ItemId) || string.IsNullOrWhiteSpace(request.ZoneId))
    {
        return Results.BadRequest(new { error = "itemId and zoneId are required." });
    }

    if (!knownZones.Contains(request.ZoneId))
    {
        return Results.BadRequest(new { error = $"Unknown zoneId '{request.ZoneId}'." });
    }

    var result = await board.MoveItemAsync(request.ItemId, request.ZoneId, ct);
    return result switch
    {
        MoveItemResult.Moved => Results.Ok(),
        MoveItemResult.Unchanged => Results.Conflict(new { error = "Item already in that zone." }),
        MoveItemResult.NotFound => Results.NotFound(new { error = "Item not found." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
});

app.MapPost("/api/drag/begin", async (BeginDragRequest request, DragSessionService drag, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.CircuitId)
        || string.IsNullOrWhiteSpace(request.SessionId)
        || request.Payload is null
        || string.IsNullOrWhiteSpace(request.Payload.ItemId))
    {
        return Results.BadRequest(new { error = "circuitId, sessionId, and payload.itemId are required." });
    }

    await drag.BeginAsync(request.CircuitId, request.SessionId, request.Payload, ct);
    return Results.Accepted();
});

app.MapPost("/api/drag/cancel", async (CancelDragRequest request, DragSessionService drag, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.CircuitId) || string.IsNullOrWhiteSpace(request.SessionId))
    {
        return Results.BadRequest(new { error = "circuitId and sessionId are required." });
    }

    var cancelled = await drag.CancelAsync(request.CircuitId, request.SessionId, ct);
    return cancelled ? Results.Accepted() : Results.NotFound(new { error = "Session not found." });
});

app.MapPost("/api/drag/accept", async (AcceptDropRequest request, DragSessionService drag, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.TargetCircuitId)
        || string.IsNullOrWhiteSpace(request.SessionId)
        || string.IsNullOrWhiteSpace(request.SourceCircuitId)
        || string.IsNullOrWhiteSpace(request.TargetZoneId)
        || request.Payload is null)
    {
        return Results.BadRequest(new { error = "targetCircuitId, sessionId, sourceCircuitId, targetZoneId, and payload are required." });
    }

    if (!knownZones.Contains(request.TargetZoneId))
    {
        return Results.BadRequest(new { error = $"Unknown targetZoneId '{request.TargetZoneId}'." });
    }

    var accepted = await drag.AcceptAsync(
        request.TargetCircuitId,
        request.SessionId,
        request.SourceCircuitId,
        request.TargetZoneId,
        request.Payload,
        ct);
    return accepted ? Results.Accepted() : Results.NotFound(new { error = "Session not found." });
});

app.MapHub<BoardRealtimeHub>("/hubs/board");

app.Run();
