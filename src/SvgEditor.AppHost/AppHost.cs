var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.SvgEditor_Api>("svgeditor-api");

builder.AddProject<Projects.SvgEditor_Web>("svgeditor-web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
