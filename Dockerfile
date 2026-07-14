# ========================================
# Interplanetary Server - Dockerfile
# ========================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Servers/CommonLib/CommonLib.csproj", "CommonLib/"]
COPY ["Servers/BaseServer/BaseServer.csproj", "BaseServer/"]
RUN dotnet restore "BaseServer/BaseServer.csproj"

# Copy source code
COPY Servers/CommonLib/ CommonLib/
COPY Servers/BaseServer/ BaseServer/

# Build and publish
WORKDIR /src/BaseServer
RUN dotnet publish -c Release -o /app/publish

# ========================================
# Runtime stage
# ========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Copy configuration template (placeholder DB values — override via
# Databases__Table__*/Databases__Auth__* env vars at run time, see docker-compose.yml)
COPY Servers/BaseServer/appsettings.example.json ./appsettings.json

# Create logs directory
RUN mkdir -p /app/logs

# The application uses its canonical port inside the container. Compose may publish
# it through a different host port (11021 by default).
EXPOSE 9000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD netstat -an | grep 9000 || exit 1

# Run server
ENTRYPOINT ["dotnet", "BaseServer.dll"]
