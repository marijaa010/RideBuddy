using Microsoft.EntityFrameworkCore;
using Serilog;
using User.API.Extensions;
using User.API.Middleware;
using User.Infrastructure.Persistence;
using User.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel: REST on 5001 (HTTP/1.1), gRPC on 50051 (HTTP/2)
builder.WebHost.ConfigureKestrel(options =>
{
    // REST API endpoint
    options.ListenAnyIP(5001, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
    // gRPC endpoint (requires HTTP/2)
    options.ListenAnyIP(50051, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
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
        Title = "RideBuddy User API",
        Version = "v1",
        Description = "Microservice for user management and authentication"
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

// Infrastructure services (Identity, EF Core, RabbitMQ)
builder.Services.AddInfrastructureServices(builder.Configuration);

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// gRPC
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
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
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
app.MapGrpcService<UserGrpcService>();
app.MapHealthChecks("/health");

Log.Information("User Service starting on port {Port}", builder.Configuration["Urls"] ?? "5001");

app.Run();
