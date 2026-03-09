# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SnapdragonApi.csproj", "."]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Run as non-root user (Cloud Run best practice)
RUN adduser --disabled-password --gecos "" appuser
WORKDIR /app

COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app
USER appuser

# Cloud Run injects PORT (default 8080); ASP.NET Core reads ASPNETCORE_HTTP_PORTS
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SnapdragonApi.dll"]
