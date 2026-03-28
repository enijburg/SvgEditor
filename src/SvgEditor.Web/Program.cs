using System.Reflection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SvgEditor.Web;
using SvgEditor.Web.Features.Canvas.CanvasPage;
using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Copilot.Services;
using SvgEditor.Web.Features.History.Models;
using SvgEditor.Web.Shared.Mediator;
using SvgEditor.Web.Shared.Storage;
using SvgEditor.Web.Shared.Validation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var assembly = Assembly.GetExecutingAssembly();

builder.Services.AddMediator(assembly);
builder.Services.AddValidators(assembly);

builder.Services.AddSingleton<EditorState>();
builder.Services.AddSingleton<HistoryStack>();
builder.Services.AddSingleton<IStorageService, BrowserStorageService>();
builder.Services.AddSingleton<AutoSaveService>();

builder.Services.AddSingleton<EditorContextBuilder>();
builder.Services.AddScoped<CopilotCommandApplier>();
builder.Services.AddHttpClient<CopilotApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["services:svgeditor-api:https:0"]
        ?? builder.Configuration["services:svgeditor-api:http:0"]
        ?? "https://localhost:7079");
});

await builder.Build().RunAsync();

