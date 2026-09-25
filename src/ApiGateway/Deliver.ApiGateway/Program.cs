using Deliver.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("api-gateway");
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Assigns the correlation id for the whole flow before the request is proxied.
app.UseServiceDefaults();
app.MapDefaultEndpoints();
app.MapReverseProxy();

await app.RunAsync();
