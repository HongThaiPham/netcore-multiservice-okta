using ServiceGateway;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpClient();

var app = builder.Build();

// Expose the gateway via port "4300"
app.Urls.Clear();
app.Urls.Add("http://localhost:4300");

app.UseRouting();

// We're routing clients to internal microservices, which many times
// will have different public and internal URLs. Gateways can help to
// keep routing centralized as a cross-cutting and customer-focused concern.
app.MapGet("/warehouse", async context =>
{
    var client = app.Services.GetService<IHttpClientFactory>().CreateClient();

    var message = context.CreateProxyHttpRequest(new Uri("http://localhost:4301/inventory"));
    var response = await client.SendAsync(message);
    await context.CopyProxyHttpResponse(response);
});

app.MapGet("/sales/sku/{sku:required}", async context =>
{
    var sku = context.Request.RouteValues["sku"];
    var client = app.Services.GetService<IHttpClientFactory>().CreateClient();
    var message = context.CreateProxyHttpRequest(new Uri($"http://localhost:4302/sku/{sku}"));
    var response = await client.SendAsync(message);
    await context.CopyProxyHttpResponse(response);
});

app.Run();