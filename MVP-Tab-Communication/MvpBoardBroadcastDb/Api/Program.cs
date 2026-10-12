using Microsoft.EntityFrameworkCore;
using MvpBoardBroadcastDb.Api.Data;
using MvpBoardBroadcastDb.Api.Models;
using MvpBoardBroadcastDb.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<BoardDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("BoardDb")
                      ?? "Data Source=board.db"));

builder.Services.AddScoped<BoardService>();

var app = builder.Build();

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var board = scope.ServiceProvider.GetRequiredService<BoardService>();
    await board.EnsureSeededAsync();
}

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

    if (!BoardService.KnownZones.Contains(request.ZoneId))
    {
        return Results.BadRequest(new { error = $"Unknown zoneId '{request.ZoneId}'." });
    }

    var result = await board.MoveItemAsync(request.ItemId, request.ZoneId, ct);
    return result switch
    {
        MoveItemResult.Moved => Results.Ok(),
        MoveItemResult.Unchanged => Results.Ok(), // idempotent — UI already moved
        MoveItemResult.NotFound => Results.NotFound(new { error = "Item not found." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
});

app.Run();
