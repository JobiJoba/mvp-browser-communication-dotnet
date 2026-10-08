using MvpDockerMessages.Web.Components;
using MvpDockerMessages.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["MessagesApi:BaseUrl"]
    ?? throw new InvalidOperationException("MessagesApi:BaseUrl is required.");

builder.Services.AddHttpClient<MessagesApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

// Circuit-scoped: list cache + BroadcastChannel bus per browser tab (Blazor circuit).
builder.Services.AddScoped<MessagesListCache>();
builder.Services.AddScoped<MessagesCacheTabBus>();
builder.Services.AddScoped<TabCloser>();

// In-process cross-circuit notify for /messages-cache-server (single Web instance).
builder.Services.AddSingleton<MessagesServerSyncBus>();

var app = builder.Build();

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

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
