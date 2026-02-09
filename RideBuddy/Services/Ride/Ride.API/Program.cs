using Microsoft.EntityFrameworkCore;
using Ride.API.Extensions;
using Ride.API.Middleware;
using Ride.Infrastructure.Persistence;
using Ride.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel: REST on 5002 (HTTP/1.1), gRPC on 50052 (HTTP/2)
builder.WebHost.ConfigureKestrel(options =>
{
    // REST API endpoint
    options.ListenAnyIP(5002, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
    // gRPC endpoint (requires HTTP/2)
    options.ListenAnyIP(50052, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "RideBuddy Ride API",
        Version = "v1",
        Description = "Microservice for managing rides"
    });

    options.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT token"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Application services (MediatR, validators)
builder.Services.AddApplicationServices();

// Infrastructure services (EF Core, gRPC, RabbitMQ)
builder.Services.AddInfrastructureServices(builder.Configuration);

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// gRPC server
builder.Services.AddGrpc();

// Health checks
builder.Services.AddHealthChecks();

// CORS
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

// Apply migrations on startup (for development)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RideDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure pipeline
app.UseMiddleware<GlobalExceptionHandler>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<RideGrpcService>();
app.MapHealthChecks("/health");

Log.Information("Ride Service starting — REST on 5002, gRPC on 50052");

app.Run();
