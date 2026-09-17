# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and csproj files to restore dependencies.
# We restore directly from ARSPlatform.API.csproj (not the .sln) to avoid
# pulling in optional projects (e.g. ARSPlatform.Tests) that may not exist
# in every deploy context.
COPY ARSPlatform.sln ./
COPY ARSPlatform.API/ARSPlatform.API.csproj ARSPlatform.API/
COPY ARSPlatform.MODEL/ARSPlatform.MODELS.csproj ARSPlatform.MODEL/
COPY ARSPlatform.REPO/ARSPlatform.REPOSITORIES.csproj ARSPlatform.REPO/
COPY ARSPlatform.SERVICE/ARSPlatform.SERVICES.csproj ARSPlatform.SERVICE/

RUN dotnet restore ARSPlatform.API/ARSPlatform.API.csproj

# Copy all source files and build
COPY . .
RUN dotnet publish ARSPlatform.API/ARSPlatform.API.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Render exposes the port in the PORT environment variable.
# We configure ASP.NET Core to listen on all interfaces at the specified PORT, defaulting to 8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ARSPlatform.API.dll"]