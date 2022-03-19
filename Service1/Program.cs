
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://localhost:4301");

app.UseRouting();

app.MapGet("/inventory", () => "Total inventory is 721 items");

app.Run();