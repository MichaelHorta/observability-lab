using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Payments.Api
{
    public static class TelemetryExtensions
    {
        public const string ActivitySourceName = "Shop.Business";
        public const string MeterName = "Shop.Business";

        public static WebApplicationBuilder AddTelemetry(
            this WebApplicationBuilder builder)
        {
            var serviceName =
                builder.Configuration["OTEL_SERVICE_NAME"]
                ?? builder.Environment.ApplicationName;

            // Unique for each process, including across pod restarts.
            var instanceId = Guid.NewGuid().ToString();

            void ConfigureResource(ResourceBuilder resource)
            {
                resource.AddService(
                    serviceName: serviceName,
                    serviceVersion: "1.0.0",
                    serviceInstanceId: instanceId);
            }

            builder.Services
                .AddOpenTelemetry()
                .ConfigureResource(ConfigureResource)
                .WithTracing(tracing => tracing
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource(ActivitySourceName)
                    .AddOtlpExporter())
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(MeterName)
                    .AddOtlpExporter());

            builder.Logging.AddOpenTelemetry(logging =>
            {
                var resource = ResourceBuilder.CreateDefault();
                ConfigureResource(resource);

                logging.SetResourceBuilder(resource);
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.AddOtlpExporter();
            });

            return builder;
        }
    }
}
