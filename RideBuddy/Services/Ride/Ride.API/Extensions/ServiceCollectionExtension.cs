using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using Ride.Application.Behaviors;
using Ride.Application.Interfaces;
using Ride.Domain.Interfaces;
using Ride.Infrastructure.Messaging;
using Ride.Infrastructure.Outbox;
using Ride.Infrastructure.Persistence;
using Ride.Infrastructure.Protos;
using Ride.Infrastructure.Repositories;
using Ride.Infrastructure.Services;

namespace Ride.API.Extensions;

/// <summary>
/// Extension methods for configuring dependency injection.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Registers Application layer services: MediatR, FluentValidation, pipeline behaviors.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(Ride.Application.Commands.CreateRide.CreateRideCommand).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    /// <summary>
    /// Registers Infrastructure layer services: EF Core, repositories, gRPC clients, RabbitMQ, outbox.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ----- EF Core (PostgreSQL) -----
        services.AddDbContext<RideDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("RideDb")));

        // ----- Repositories -----
        services.AddScoped<IRideRepository, RideRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ----- gRPC Clients (Ride calls User to validate drivers) -----
        services.AddGrpcClient<UserGrpc.UserGrpcClient>(options =>
        {
            options.Address = new Uri(configuration["GrpcServices:UserService"]!);
        });

        services.AddScoped<IUserGrpcClient, UserGrpcClient>();

        // ----- RabbitMQ -----
        services.AddSingleton<IConnection>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            try
            {
                return factory.CreateConnection();
            }
            catch (Exception ex)
            {
                var logger = sp.GetRequiredService<ILogger<RabbitMqEventPublisher>>();
                logger.LogWarning(ex, "Could not connect to RabbitMQ on startup. Outbox processor will retry.");
                return null!;
            }
        });

        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();

        // ----- Outbox background processor -----
        services.AddHostedService<OutboxProcessor>();

        return services;
    }

    /// <summary>
    /// Configures JWT Bearer authentication using the same symmetric key as User Service.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        
        // Read secret key from environment variable first, fallback to appsettings
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
            ?? jwtSettings.GetSection("secretKey").Value;

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT Secret Key is not configured. Set JWT_SECRET_KEY environment variable or add JwtSettings:secretKey in appsettings.json");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.GetSection("validIssuer").Value,
                ValidAudience = jwtSettings.GetSection("validAudience").Value,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };

            if (string.Equals(
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    "Development",
                    StringComparison.OrdinalIgnoreCase))
            {
                options.RequireHttpsMetadata = false;
            }
        });

        return services;
    }
}
