var builder = WebApplication.CreateBuilder(args);

// Allow requests from the Blazor application.
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5024",
                "https://localhost:7278")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRouting();
app.UseCors("BlazorClient");

app.MapReverseProxy();

app.Run();
