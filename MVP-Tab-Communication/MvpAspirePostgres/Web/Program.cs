using MvpAspirePostgres.Web.Components;
using MvpAspirePostgres.Web.Services.Api;
using MvpAspirePostgres.Web.Services.Board;
using MvpAspirePostgres.Web.Services.DragDrop;
using MvpAspirePostgres.Web.Services.DragSession;
using MvpAspirePostgres.Web.Services.Instance;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IAppInstanceInfo, AppInstanceInfo>();

builder.Services.AddHttpClient<BoardApiClient>(client =>
{
    // Aspire service discovery name of the Api resource in AppHost.
    client.BaseAddress = new Uri("https+http://api");
});

builder.Services.AddSingleton<ApiRealtimeConnection>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ApiRealtimeConnection>());

builder.Services.AddSingleton<ApiBoardStore>();
builder.Services.AddSingleton<IBoardStore>(sp => sp.GetRequiredService<ApiBoardStore>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ApiBoardStore>());

builder.Services.AddSingleton<ApiDragSessionHub>();
builder.Services.AddSingleton<IDragSessionHub>(sp => sp.GetRequiredService<ApiDragSessionHub>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ApiDragSessionHub>());

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

app.MapGet("/api/instance", (IAppInstanceInfo instance) =>
    Results.Ok(new { instance.ShortId, instance.ReplicaIndex }));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
