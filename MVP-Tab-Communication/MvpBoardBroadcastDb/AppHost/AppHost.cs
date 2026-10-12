var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.MvpBoardBroadcastDb_Api>("api")
    .WithHttpEndpoint(port: 5405, name: "http", isProxied: false)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5405")
    .WithEnvironment("ASPNETCORE_HTTPS_PORT", "");

builder.AddProject<Projects.MvpBoardBroadcastDb_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5400, name: "http", isProxied: false)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5400")
    .WithEnvironment("ASPNETCORE_HTTPS_PORT", "")
    .WithEnvironment("ApiBaseUrl", "http://127.0.0.1:5405");

builder.Build().Run();
