using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Application.Services;
using Notification.Infrastructure.Email;
using Notification.Infrastructure.Grpc;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Protos;
using Notification.Infrastructure.RealTime;
using RabbitMQ.Client;

namespace Notification.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("NotificationDb")));

        // Repository
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Application service
        services.AddScoped<NotificationService>();

        // Email (SMTP / MailKit)
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // gRPC client to User Service
        services.AddGrpcClient<UserGrpc.UserGrpcClient>(options =>
        {
            options.Address = new Uri(configuration["GrpcServices:UserService"]
                ?? "http://localhost:50051");
        });
        services.AddScoped<IUserGrpcClient, UserGrpcClient>();

        // SignalR
        services.AddSignalR();
        services.AddScoped<IRealTimeNotifier, SignalRNotifier>();

        // RabbitMQ connection
        services.AddSingleton<IConnection?>(sp =>
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = configuration["RabbitMQ:Username"] ?? "guest",
                    Password = configuration["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/"
                };
                return factory.CreateConnection();
            }
            catch (Exception ex)
            {
                var logger = sp.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RabbitMQ");
                logger.LogWarning(ex, "Could not connect to RabbitMQ. Consumer will not start.");
                return null;
            }
        });

        // Background consumer
        services.AddHostedService<BookingEventConsumer>();

        return services;
    }
}
