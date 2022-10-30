var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.UseStaticFiles();
app.Urls.Add("http://localhost:5000");

app.Run();
