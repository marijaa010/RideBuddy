# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SharedKernel/SharedKernel.csproj", "SharedKernel/"]
COPY ["Services/User/User.API/User.API.csproj", "Services/User/User.API/"]
COPY ["Services/User/User.Application/User.Application.csproj", "Services/User/User.Application/"]
COPY ["Services/User/User.Infrastructure/User.Infrastructure.csproj", "Services/User/User.Infrastructure/"]
COPY ["Services/User/User.Domain/User.Domain.csproj", "Services/User/User.Domain/"]

# Restore dependencies
RUN dotnet restore "Services/User/User.API/User.API.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR "/src/Services/User/User.API"
RUN dotnet build "User.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "User.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 5001 50051

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "User.API.dll"]
