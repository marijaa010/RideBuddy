# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Services/Ride/Ride.API/Ride.API.csproj", "Services/Ride/Ride.API/"]
COPY ["Services/Ride/Ride.Application/Ride.Application.csproj", "Services/Ride/Ride.Application/"]
COPY ["Services/Ride/Ride.Infrastructure/Ride.Infrastructure.csproj", "Services/Ride/Ride.Infrastructure/"]
COPY ["Services/Ride/Ride.Domain/Ride.Domain.csproj", "Services/Ride/Ride.Domain/"]
COPY ["SharedKernel/SharedKernel.csproj", "SharedKernel/"]

# Restore dependencies
RUN dotnet restore "Services/Ride/Ride.API/Ride.API.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR "/src/Services/Ride/Ride.API"
RUN dotnet build "Ride.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Ride.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5002 50052

ENV ASPNETCORE_URLS=http://+:5002
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ride.API.dll"]
