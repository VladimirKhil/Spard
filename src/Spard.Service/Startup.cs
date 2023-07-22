using AspNetCoreRateLimit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using Spard.Service.BackgroundServices;
using Spard.Service.Contract;
using Spard.Service.Helpers;
using Spard.Service.Implementation;
using Spard.Service.Metrics;

namespace Spard.Service;

public sealed class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IExamplesRepository, ExamplesRepository>();
        services.AddSingleton<ITransformManager, TransformManager>();

        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new TimeSpanToStringConverter()));

        services.AddHostedService<ExamplesLoader>();

        AddRateLimits(services, Configuration);
        AddMetrics(services);
    }

    private static void AddRateLimits(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimit"));

        services.AddMemoryCache();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddInMemoryRateLimiting();
    }

    private static void AddMetrics(IServiceCollection services)
    {
        var meters = new OtelMetrics();

        services.AddOpenTelemetry().WithMetrics(builder =>
            builder
                .ConfigureResource(rb => rb.AddService("Spard"))
                .AddMeter(meters.MeterName)
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddPrometheusExporter());

        services.AddSingleton(meters);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSerilogRequestLogging();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        app.UseIpRateLimiting();

        app.UseOpenTelemetryPrometheusScrapingEndpoint();
    }
}
