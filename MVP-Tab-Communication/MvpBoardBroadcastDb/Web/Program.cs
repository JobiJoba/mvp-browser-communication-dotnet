using MvpBoardBroadcastDb.Web.Components;
using MvpBoardBroadcastDb.Web.Services.Api;
using MvpBoardBroadcastDb.Web.Services.CrossTab;
using MvpBoardBroadcastDb.Web.Services.DragDrop;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<BoardApiClient>(client =>
{
    // Aspire service discovery when run via AppHost; falls back for standalone.
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "https+http://api";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<ICrossTabBus, CrossTabBus>();
builder.Services.AddScoped<DragDropCoordinator>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
