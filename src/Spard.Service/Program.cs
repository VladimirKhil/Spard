using AspNetCoreRateLimit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using Spard.Service.BackgroundServices;
using Spard.Service.Configuration;
using Spard.Service.Contracts;
using Spard.Service.EndpointDefinitions;
using Spard.Service.Helpers;
using Spard.Service.Metrics;
using Spard.Service.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Filter.ByExcluding(logEvent =>
        logEvent.Exception is BadHttpRequestException
        || logEvent.Exception is OperationCanceledException));

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();

Configure(app);

app.Run();

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<SpardOptions>(configuration.GetSection(SpardOptions.ConfigurationSectionName));

    services.AddSingleton<IExamplesRepository, ExamplesRepository>();
    services.AddSingleton<ITransformManager, TransformManager>();

    services.AddHostedService<ExamplesLoader>();

    AddRateLimits(services, configuration);
    AddMetrics(services);
}

static void AddRateLimits(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimit"));

    services.AddMemoryCache();
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    services.AddInMemoryRateLimiting();
}

static void AddMetrics(IServiceCollection services)
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

static void Configure(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    ExamplesEndpointDefinitions.DefineExamplesEndpoint(app);
    SpardEndpointDefinitions.DefineExamplesEndpoint(app);
    TransformEndpointDefinitions.DefineExamplesEndpoint(app);

    app.UseIpRateLimiting();

    app.UseOpenTelemetryPrometheusScrapingEndpoint();
}