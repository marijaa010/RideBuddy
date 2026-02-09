using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Ocelot configuration
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Ocelot services
builder.Services.AddOcelot(builder.Configuration);

// CORS — allow any origin for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Middleware that runs BEFORE Ocelot takes over the pipeline
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.TrimEnd('/') ?? "";

    switch (path)
    {
        case "/health":
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("Healthy");
            return;

        case "/swagger":
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(SwaggerPage());
            return;

        case "":
            context.Response.Redirect("/swagger");
            return;

        default:
            await next();
            break;
    }
});

Log.Information("API Gateway starting on port 5000");

await app.UseOcelot();

app.Run();

// Minimal aggregated Swagger page pointing to each service
static string SwaggerPage() => """
<!DOCTYPE html>
<html>
<head>
    <title>RideBuddy API Gateway</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 700px; margin: 60px auto; color: #333; }
        h1 { color: #2c3e50; }
        a { color: #3498db; text-decoration: none; }
        a:hover { text-decoration: underline; }
        .service { background: #f8f9fa; border-radius: 8px; padding: 20px; margin: 16px 0; border-left: 4px solid #3498db; }
        .service h3 { margin-top: 0; }
        code { background: #e9ecef; padding: 2px 6px; border-radius: 4px; font-size: 0.9em; }
    </style>
</head>
<body>
    <h1>RideBuddy API Gateway</h1>
    <p>All services are accessible through this gateway on <code>/api/*</code> routes.</p>

    <div class="service">
        <h3>User Service</h3>
        <p><code>POST /api/auth/register</code> &middot; <code>POST /api/auth/login</code> &middot; <code>GET /api/users/{id}</code></p>
        <p><a href="http://localhost:5001/swagger" target="_blank">Swagger UI</a></p>
    </div>

    <div class="service">
        <h3>Ride Service</h3>
        <p><code>POST /api/rides</code> &middot; <code>GET /api/rides/search</code> &middot; <code>GET /api/rides/{id}</code></p>
        <p><a href="http://localhost:5002/swagger" target="_blank">Swagger UI</a></p>
    </div>

    <div class="service">
        <h3>Booking Service</h3>
        <p><code>POST /api/bookings</code> &middot; <code>GET /api/bookings/my-bookings</code> &middot; <code>GET /api/bookings/{id}</code></p>
        <p><a href="http://localhost:5003/swagger" target="_blank">Swagger UI</a></p>
    </div>

    <hr style="margin-top: 40px; border: none; border-top: 1px solid #dee2e6;" />
    <p style="color: #6c757d; font-size: 0.85em;">Gateway health: <a href="/health">/health</a></p>
</body>
</html>
""";
