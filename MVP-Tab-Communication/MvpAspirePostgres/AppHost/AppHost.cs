var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var mvpdb = postgres.AddDatabase("mvpdb");

var api = builder.AddProject<Projects.MvpAspirePostgres_Api>("api")
    .WithReference(mvpdb)
    .WaitFor(mvpdb)
    .WithHttpEndpoint(port: 5295, name: "http", isProxied: false)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5295")
    .WithEnvironment("ASPNETCORE_HTTPS_PORT", "");

// Same .csproj twice with fixed proxyless ports → two stable URLs for A/B testing.
// Frontends talk to the Api only (HTTP + SignalR) — no direct Postgres access.
builder.AddProject<Projects.MvpAspirePostgres_Web>("web-a")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5290, name: "http", isProxied: false)
    .WithEnvironment("MVP_DEMO_REPLICA", "A")
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5290")
    .WithEnvironment("ASPNETCORE_HTTPS_PORT", "")
    .WithEnvironment("BoardApi__BaseUrl", "http://127.0.0.1:5295");

builder.AddProject<Projects.MvpAspirePostgres_Web>("web-b")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5291, name: "http", isProxied: false)
    .WithEnvironment("MVP_DEMO_REPLICA", "B")
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5291")
    .WithEnvironment("ASPNETCORE_HTTPS_PORT", "")
    .WithEnvironment("BoardApi__BaseUrl", "http://127.0.0.1:5295");

builder.Build().Run();
