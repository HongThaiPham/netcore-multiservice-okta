using Okta.AspNetCore;
using ServiceGateway;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpClient();

// Configure authentication middleware
AddAuthMiddleware(builder);

var app = builder.Build();

// Expose the gateway via port "4300"
app.Urls.Clear();
app.Urls.Add("http://localhost:4300");

// Federated Logging
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request for \"{context.Request.Path}\"");
    await next();
});

// Federated Rate-Limiting
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Rate-Limit-Limit", "infinite");
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Using Okta to generate tokens by forwarding the request.
// To your clients, if you strip out Okta specific response headers,
// they would have no idea that Okta is being used as the identity
// provider!
app.MapPost("/token", async context =>
{
    var client = app.Services.GetService<IHttpClientFactory>().CreateClient();

    var message = context.CreateProxyHttpRequest(new Uri($"{builder.Configuration["Okta:Domain"]}/oauth2/default/v1/token"));
    var response = await client.SendAsync(message);
    await context.CopyProxyHttpResponse(response);
});

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
}).RequireAuthorization();

app.Run();


void AddAuthMiddleware(WebApplicationBuilder builder)
{
    // This method is all it takes to use Okta to automatically test incoming
    // bearer tokens for authentication!
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = OktaDefaults.ApiAuthenticationScheme;
        options.DefaultChallengeScheme = OktaDefaults.ApiAuthenticationScheme;
        options.DefaultSignInScheme = OktaDefaults.ApiAuthenticationScheme;
    })
    .AddOktaWebApi(new OktaWebApiOptions()
    {
        OktaDomain = builder.Configuration["Okta:Domain"],
    });

    builder.Services.AddAuthorization();
}