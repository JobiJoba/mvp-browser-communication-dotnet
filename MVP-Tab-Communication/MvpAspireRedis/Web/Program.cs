using MvpAspireRedis.Web.Components;
using MvpAspireRedis.Web.Services.Board;
using MvpAspireRedis.Web.Services.DragDrop;
using MvpAspireRedis.Web.Services.DragSession;
using MvpAspireRedis.Web.Services.Instance;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("redis");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IAppInstanceInfo, AppInstanceInfo>();
builder.Services.AddSingleton<RedisBoardStore>();
builder.Services.AddSingleton<IBoardStore>(sp => sp.GetRequiredService<RedisBoardStore>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<RedisBoardStore>());

builder.Services.AddSingleton<RedisDragSessionHub>();
builder.Services.AddSingleton<IDragSessionHub>(sp => sp.GetRequiredService<RedisDragSessionHub>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<RedisDragSessionHub>());

builder.Services.AddScoped<DragDropCoordinator>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// AppHost binds HTTP-only on fixed ports (5280 / 5281). Never redirect to HTTPS
// from launchSettings (7182) — that port is not listening and browsers time out.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapGet("/api/instance", (IAppInstanceInfo instance) =>
    Results.Ok(new { instance.ShortId, instance.ReplicaIndex }));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
