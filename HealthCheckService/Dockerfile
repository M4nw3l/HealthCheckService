# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 9090


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HealthCheckService.Telemetry/HealthCheckService.Telemetry.csproj", "HealthCheckService.Telemetry/"]
COPY ["HealthCheckService/HealthCheckService.csproj", "HealthCheckService/"]
RUN dotnet restore "./HealthCheckService.Telemetry/HealthCheckService.Telemetry.csproj"
RUN dotnet restore "./HealthCheckService/HealthCheckService.csproj"
COPY . .
RUN dotnet build "./HealthCheckService/HealthCheckService.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:Docker=true

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
RUN dotnet publish "./HealthCheckService/HealthCheckService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:Docker=true

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HealthCheckService.dll"]