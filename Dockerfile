# ── Stage 1: Build ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (for layer caching)
COPY FULLEYMTAXTASK.sln ./
COPY Eymta.core/Eymta.core.csproj                   Eymta.core/
COPY Eymta.Application/Eymta.Application.csproj     Eymta.Application/
COPY Eymta.Repository/Eymta.Repository.csproj       Eymta.Repository/
COPY EymtaXFull/EymtaXFull.csproj                   EymtaXFull/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Publish release build
RUN dotnet publish EymtaXFull/EymtaXFull.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create folder for uploaded files
RUN mkdir -p /app/wwwroot/uploads

COPY --from=build /app/publish .

# Render injects PORT env var automatically
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "EymtaXFull.dll"]
