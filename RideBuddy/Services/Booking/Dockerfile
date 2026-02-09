# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SharedKernel/SharedKernel.csproj", "SharedKernel/"]
COPY ["Services/Booking/Booking.API/Booking.API.csproj", "Services/Booking/Booking.API/"]
COPY ["Services/Booking/Booking.Application/Booking.Application.csproj", "Services/Booking/Booking.Application/"]
COPY ["Services/Booking/Booking.Infrastructure/Booking.Infrastructure.csproj", "Services/Booking/Booking.Infrastructure/"]
COPY ["Services/Booking/Booking.Domain/Booking.Domain.csproj", "Services/Booking/Booking.Domain/"]

# Restore dependencies
RUN dotnet restore "Services/Booking/Booking.API/Booking.API.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR "/src/Services/Booking/Booking.API"
RUN dotnet build "Booking.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Booking.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 5003

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booking.API.dll"]