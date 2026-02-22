# RideBuddy

A carpooling platform where drivers can offer rides and passengers can search and book seats. Built as a microservices application with a .NET 8 backend and an Angular 18 frontend.

This is a student project for the course **Razvoj softvera 2**.

**Team members:** Marija Markovic, Dragana Zdravkovic, Marko Savic

## Features

- **User registration and authentication** - JWT-based auth with role support
- **Ride management** - drivers can create rides with origin, destination, departure time, price, and seat count
O- **Booking system** - passengers book seats on available rides, drivers can enable auto-confirm or manually approve/reject bookings
- **Real-time notifications** - in-app notifications via SignalR WebSockets, plus email notifications via SMTP
- **Email alerts** - passengers and drivers get email notifications on booking events (created, confirmed, rejected, cancelled, completed)

## Architecture

The backend is split into four independent services that communicate through gRPC (synchronous) and RabbitMQ (asynchronous events). Each service has its own PostgreSQL database.

```
Frontend (Angular 18)
    |
API Gateway (YARP reverse proxy, port 5000)
    |
    +-- User Service       (port 5001, gRPC 50051)
    +-- Ride Service       (port 5002, gRPC 50052)
    +-- Booking Service    (port 5003)
    +-- Notification Service (port 5004, SignalR WebSocket)
```

- **User Service** - handles registration, login, JWT token issuing, and user profile management
- **Ride Service** - CRUD for rides, seat availability
- **Booking Service** - manages the booking lifecycle (create, confirm, reject, cancel, complete) with a saga pattern and transactional outbox for event publishing
- **Notification Service** - consumes booking events from RabbitMQ and delivers notifications through three channels: database (in-app), SignalR (real-time push), and email (SMTP via MailKit)
- **API Gateway** - routes all frontend HTTP requests to the appropriate backend service

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 18, TypeScript, SCSS |
| Backend | .NET 8, ASP.NET Core, Entity Framework Core |
| Databases | PostgreSQL 16 |
| Messaging | RabbitMQ 3.13 |
| Inter-service | gRPC, Protocol Buffers |
| Real-time | SignalR |
| Email | MailKit (SMTP) |
| Auth | JWT Bearer tokens |
| Containerization | Docker, Docker Compose |

## Prerequisites

- [Docker](https://www.docker.com/) and Docker Compose (included with Docker Desktop)
- An SMTP account for email notifications (e.g. Gmail with an App Password)

## Getting Started

1. **Clone the repository**

```bash
git clone https://github.com/marijaa010/RideBuddy.git
cd RideBuddy
```

2. **Set up environment variables**

Copy the example env file and fill in your values:

```bash
cp .env.example .env
```

Open `.env` and set:
- `POSTGRES_PASSWORD` - password for the PostgreSQL databases
- `RABBITMQ_PASSWORD` - password for the RabbitMQ broker
- `JWT_SECRET_KEY` - a long random string used to sign JWT tokens
- `SMTP_*` - your SMTP server details (for Gmail, use an [App Password](https://support.google.com/accounts/answer/185833))

3. **Build and run**

```bash
docker compose up --build -d
```

This starts all services, databases, and the message broker. First build takes a few minutes.

4. **Open the app**

Once all containers are healthy, go to [http://localhost:8080](http://localhost:8080).

## Ports

| Service | URL |
|---------|-----|
| Frontend | http://localhost:8080 |
| API Gateway | http://localhost:5000 |
| User Service (Swagger) | http://localhost:5001/swagger |
| Ride Service (Swagger) | http://localhost:5002/swagger |
| Booking Service (Swagger) | http://localhost:5003/swagger |
| Notification Service (Swagger) | http://localhost:5004/swagger |
| RabbitMQ Management UI | http://localhost:15672 |

## Project Structure

```
RideBuddy/
  ApiGateway/              - YARP reverse proxy
  Services/
    User/                  - User Service (Domain, Application, Infrastructure, API)
    Ride/                  - Ride Service
    Booking/               - Booking Service
    Notification/          - Notification Service
  frontend/                - Angular 18 SPA
  docker-compose.yml
  .env.example
```
