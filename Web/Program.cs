using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Web;
using Web.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(
    sp => new HttpClient
    {
        BaseAddress = new Uri("https://localhost:7102/")
    });

// HTTP client for the AuthenticationService microservice.
builder.Services.AddHttpClient(
    "AuthenticationService",
    client =>
    {
        client.BaseAddress =
            new Uri("https://localhost:7006/");
    });

// Register Blazor authentication services.
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<
    JwtAuthenticationStateProvider>();

builder.Services.AddScoped<
    AuthenticationStateProvider>(
        provider =>
            provider.GetRequiredService<
                JwtAuthenticationStateProvider>());

await builder.Build().RunAsync();