using Booking.Application.Behaviors;
using Booking.Application.Interfaces;
using Booking.Domain.Interfaces;
using Booking.Infrastructure.Messaging;
using Booking.Infrastructure.Outbox;
using Booking.Infrastructure.Persistence;
using Booking.Infrastructure.Protos;
using Booking.Infrastructure.Repositories;
using Booking.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using System.Text;

namespace Booking.API.Extensions;

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
        var applicationAssembly = typeof(Booking.Application.Commands.CreateBooking.CreateBookingCommand).Assembly;

        // MediatR — command/query handlers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));

        // FluentValidation — command validators
        services.AddValidatorsFromAssembly(applicationAssembly);

        // Pipeline behaviors (order matters: validation runs before logging)
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
        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BookingDb")));

        // ----- Repositories -----
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ----- gRPC Clients -----
        services.AddGrpcClient<RideGrpc.RideGrpcClient>(options =>
        {
            options.Address = new Uri(configuration["GrpcServices:RideService"]!);
        });

        services.AddGrpcClient<UserGrpc.UserGrpcClient>(options =>
        {
            options.Address = new Uri(configuration["GrpcServices:UserService"]!);
        });

        services.AddScoped<IRideGrpcClient, RideGrpcClient>();
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
                // Allow the app to start even if RabbitMQ is not yet available
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
    /// Configures JWT Bearer authentication.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = configuration["Jwt:Authority"];
            options.Audience = configuration["Jwt:Audience"];

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            // Allow HTTP in development (gRPC + identity server on localhost)
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