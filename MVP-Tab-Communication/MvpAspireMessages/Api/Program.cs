using MvpAspireMessages.Api.Models;
using MvpAspireMessages.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddSingleton<MessageStore>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGet("/api/messages", (MessageStore store) => Results.Ok(store.GetAll()));

app.MapGet("/api/messages/{id:guid}", (Guid id, MessageStore store) =>
{
    var message = store.Get(id);
    return message is null ? Results.NotFound() : Results.Ok(message);
});

app.MapPut("/api/messages/{id:guid}/state", (Guid id, UpdateMessageStateRequest request, MessageStore store) =>
{
    var updated = store.UpdateState(id, request.State);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.Run();
