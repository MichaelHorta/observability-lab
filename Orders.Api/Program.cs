using Orders.Api;
using System.Diagnostics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.AddTelemetry();

builder.Services.AddHttpClient("inventory", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["INVENTORY_URL"]
        ?? "http://localhost:5002");

    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("payments", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["PAYMENTS_URL"]
        ?? "http://localhost:5003");

    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

using var activities =
    new ActivitySource(TelemetryExtensions.ActivitySourceName);

using var meter = new Meter(TelemetryExtensions.MeterName);

var completedOrders = meter.CreateCounter<long>(
    "shop.orders.completed",
    description: "Completed order attempts by outcome");

var orderDuration = meter.CreateHistogram<double>(
    "shop.order.duration",
    unit: "s",
    description: "Order processing duration");

app.MapGet("/health", () => Results.Ok());

app.MapPost("/orders", async (
    OrderRequest request,
    IHttpClientFactory clients,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    if (request.Scenario is not ("normal" or "slow" or "fail"))
    {
        return Results.BadRequest(new
        {
            error = "Scenario must be normal, slow, or fail."
        });
    }

    var orderId = Guid.NewGuid();
    var started = Stopwatch.GetTimestamp();
    var outcome = "failed";

    using var activity = activities.StartActivity("order.create");
    activity?.SetTag("order.id", orderId);

    var traceId = Activity.Current?.TraceId.ToString();

    using var scope = logger.BeginScope(
        new Dictionary<string, object>
        {
            ["OrderId"] = orderId
        });

    try
    {
        logger.LogInformation(
            "Starting order with scenario {Scenario}",
            request.Scenario);

        var inventoryClient = clients.CreateClient("inventory");

        using var inventoryResponse = await inventoryClient.GetAsync(
            "/inventory/demo-sku",
            cancellationToken);

        inventoryResponse.EnsureSuccessStatusCode();

        var paymentsClient = clients.CreateClient("payments");

        using var paymentResponse = await paymentsClient.PostAsJsonAsync(
            "/payments",
            new
            {
                OrderId = orderId,
                Scenario = request.Scenario
            },
            cancellationToken);

        paymentResponse.EnsureSuccessStatusCode();

        outcome = "success";

        logger.LogInformation("Order completed");

        return Results.Ok(new
        {
            orderId,
            status = "completed",
            traceId
        });
    }
    catch (Exception exception)
        when (exception is HttpRequestException or TaskCanceledException)
    {
        activity?.SetStatus(
            ActivityStatusCode.Error,
            exception.Message);

        logger.LogError(exception, "Order failed");

        return Results.Json(
            new
            {
                orderId,
                status = "failed",
                traceId
            },
            statusCode: StatusCodes.Status502BadGateway);
    }
    finally
    {
        var tags = new TagList
        {
            { "outcome", outcome }
        };

        completedOrders.Add(1, tags);

        orderDuration.Record(
            Stopwatch.GetElapsedTime(started).TotalSeconds,
            tags);
    }
});

app.Run();

record OrderRequest(string Scenario);