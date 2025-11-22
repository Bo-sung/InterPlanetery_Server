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

# Copy configuration template
COPY Servers/BaseServer/appsettings.example.json ./appsettings.json

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 11021

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD netstat -an | grep 11021 || exit 1

# Run server
ENTRYPOINT ["dotnet", "BaseServer.dll"]
