using MvpAspirePostgres.Api.Hubs;
using MvpAspirePostgres.Api.Models;
using MvpAspirePostgres.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("mvpdb");

builder.Services.AddSignalR();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<DragSessionService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Seed schema early so the first board GET is fast.
_ = app.Services.GetRequiredService<BoardService>().EnsureReadyAsync();

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
    var moved = await board.MoveItemAsync(request.ItemId, request.ZoneId, ct);
    return moved ? Results.Ok() : Results.Conflict();
});

app.MapPost("/api/drag/begin", async (BeginDragRequest request, DragSessionService drag, CancellationToken ct) =>
{
    await drag.BeginAsync(request.CircuitId, request.SessionId, request.Payload, ct);
    return Results.Accepted();
});

app.MapPost("/api/drag/cancel", async (CancelDragRequest request, DragSessionService drag, CancellationToken ct) =>
{
    await drag.CancelAsync(request.CircuitId, request.SessionId, ct);
    return Results.Accepted();
});

app.MapPost("/api/drag/accept", async (AcceptDropRequest request, DragSessionService drag, CancellationToken ct) =>
{
    await drag.AcceptAsync(
        request.TargetCircuitId,
        request.SessionId,
        request.SourceCircuitId,
        request.TargetZoneId,
        request.Payload,
        ct);
    return Results.Accepted();
});

app.MapHub<BoardRealtimeHub>("/hubs/board");


app.Run();
