using Payments.Api;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.AddTelemetry();

var app = builder.Build();

using var activities =
    new ActivitySource(TelemetryExtensions.ActivitySourceName);

app.MapGet("/health", () => Results.Ok());

app.MapPost("/payments", async (
    PaymentRequest request,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    using var activity = activities.StartActivity("payment.authorize");
    activity?.SetTag("order.id", request.OrderId);
    activity?.SetTag("payment.scenario", request.Scenario);

    var delay = request.Scenario == "slow" ? 2000 : 120;
    await Task.Delay(delay, cancellationToken);

    if (request.Scenario == "fail")
    {
        activity?.SetStatus(
            ActivityStatusCode.Error,
            "Simulated payment provider failure");

        logger.LogError(
            "Payment provider unavailable for order {OrderId}",
            request.OrderId);

        return Results.Problem(
            title: "Simulated payment provider failure",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    logger.LogInformation(
        "Payment authorized for order {OrderId}",
        request.OrderId);

    return Results.Ok(new
    {
        request.OrderId,
        authorized = true
    });
});

app.Run();

record PaymentRequest(Guid OrderId, string Scenario);