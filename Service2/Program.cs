var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://localhost:4302");

app.UseRouting();

app.MapGet("/sku/{sku:required}", (string sku) => $"You're looking at SKU {sku}!");

app.Run();