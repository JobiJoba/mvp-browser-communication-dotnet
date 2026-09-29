using MvpServerSync.Components;
using MvpServerSync.Services.Board;
using MvpServerSync.Services.DragDrop;
using MvpServerSync.Services.DragSession;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IBoardStore, BoardStore>();
builder.Services.AddSingleton<IDragSessionHub, DragSessionHub>();
builder.Services.AddScoped<DragDropCoordinator>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
