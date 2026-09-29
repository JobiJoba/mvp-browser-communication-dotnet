using MvpCaseSidePanelBroadcast.Components;
using MvpCaseSidePanelBroadcast.Services.Cases;
using MvpCaseSidePanelBroadcast.Services.CrossTab;
using MvpCaseSidePanelBroadcast.Services.DragDrop;
using MvpCaseSidePanelBroadcast.Services.SidePanel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ICaseCatalog, CaseCatalog>();
builder.Services.AddScoped<ICrossTabBus, CrossTabBus>();
builder.Services.AddScoped<SidePanelState>();
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
