var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

// Same .csproj twice with fixed proxyless ports → two stable URLs for A/B testing.
// Clear ASPNETCORE_HTTPS_PORT so Kestrel/launchSettings cannot redirect HTTP → :7182.
builder.AddProject<Projects.MvpAspireRedis_Web>("web-a")
    .WithReference(redis)
    .WithHttpEndpoint(port: 5280, name: "http", isProxied: false)
    .WithEnvironment("MVP_DEMO_REPLICA", "A")
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5280")
    .WithEnvironment("ASPNETCORE_HTTPS_PORT", "");

builder.AddProject<Projects.MvpAspireRedis_Web>("web-b")
    .WithReference(redis)
    .WithHttpEndpoint(port: 5281, name: "http", isProxied: false)
    .WithEnvironment("MVP_DEMO_REPLICA", "B")
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5281")
    .WithEnvironment("ASPNETCORE_HTTPS_PORT", "");

builder.Build().Run();
