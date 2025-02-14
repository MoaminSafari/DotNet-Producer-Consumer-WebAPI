var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapGet("/", context =>
{
    context.Response.Redirect("/api/broker/queue");
    Logger.Log(Logger.LogLevel.Info, "Test API endpoint called");
    return Task.CompletedTask;
});

Logger.Log(Logger.LogLevel.Info, "Message Broker Started...");
app.Run();
