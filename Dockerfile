# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY McpRbacDemo/McpRbacDemo.csproj McpRbacDemo/
RUN dotnet restore McpRbacDemo/McpRbacDemo.csproj

COPY McpRbacDemo/ McpRbacDemo/
RUN dotnet publish McpRbacDemo/McpRbacDemo.csproj -c Release -o /app --no-restore

# Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./

# Runs as a non-root user; the SQLite file lives in a writable directory.
RUN useradd --create-home --uid 1001 mcp && mkdir -p /data && chown -R mcp:mcp /data /app
USER mcp

ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__DefaultConnection="Data Source=/data/mcprbac.db"
EXPOSE 8080

ENTRYPOINT ["dotnet", "McpRbacDemo.dll"]
