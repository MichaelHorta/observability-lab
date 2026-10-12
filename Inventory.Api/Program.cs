using Inventory.Api;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.AddTelemetry();
var app = builder.Build();

using var activities = new ActivitySource(TelemetryExtensions.ActivitySourceName);

app.MapGet("/health", () => Results.Ok());

app.MapGet("/inventory/{sku}", async (string sku, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    using var activity = activities.StartActivity("inventory.check");
    activity?.SetTag("product.sku", sku);

    await Task.Delay(80, cancellationToken);

    logger.LogInformation("Inventory checked for {Sku}; avalilable: {Available}", sku, true);

    return Results.Ok(
        new
        {
            sku,
            available = true
        }
    );
});

app.Run();
