# RideBuddy

A carpooling system that connects drivers and passengers traveling in the same direction to share transportation costs.

## Features

- **Drivers** can offer rides with departure, destination, date, time, and available seats
- **Passengers** can search for rides and book seats
- **Notifications** for booking confirmations and cancellations

## Architecture

Microservices architecture with the following services:

| Service | Description | Status |
|---------|-------------|--------|
| **User Service** | Registration, login, JWT authentication | Planned (Sprint 2) |
| **Ride Service** | Create and manage rides | Planned (Sprint 2) |
| **Booking Service** | Book and manage reservations | ✅ Implemented |
| **Notification Service** | Email notifications | Planned (Sprint 3) |
| **API Gateway** | Ocelot routing and auth | Planned (Sprint 3) |

### Tech Stack

- **Backend**: ASP.NET Core 8, Entity Framework Core
- **Database**: PostgreSQL
- **Messaging**: RabbitMQ
- **Inter-service Communication**: gRPC
- **Authentication**: JWT
- **Patterns**: DDD, CQRS, MediatR, Outbox Pattern

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Run Infrastructure

```bash
cd RideBuddy
docker compose up -d
```

This starts:
- PostgreSQL (port 5434)
- RabbitMQ (port 5672, management UI at http://localhost:15672)

### Run Booking Service

```bash
cd RideBuddy
dotnet run --project Services/Booking/Booking.API
```

API available at: http://localhost:5270/swagger

### API Endpoints (Booking Service)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/bookings` | Create a booking |
| GET | `/api/bookings/{id}` | Get booking by ID |
| GET | `/api/bookings/my-bookings` | Get user's bookings |
| GET | `/api/bookings/by-ride/{rideId}` | Get bookings for a ride |
| PUT | `/api/bookings/{id}/cancel` | Cancel a booking |

## Project Structure

```
RideBuddy/
├── Services/
│   └── Booking/
│       ├── Booking.API/           # REST API, Controllers
│       ├── Booking.Application/   # CQRS Commands, Queries, DTOs
│       ├── Booking.Domain/        # Entities, Value Objects, Events
│       └── Booking.Infrastructure/# EF Core, Repositories, gRPC
├── docker-compose.yml             # Infrastructure services
└── diagrams/                      # Architecture diagrams (Mermaid)
```

## Team

- Member 1: User Service, API Gateway
- Member 2: Ride Service, Frontend
- Member 3: Booking Service, Notification Service

## License

This project is for educational purposes.
