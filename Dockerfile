FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY /src .
COPY /Directory.Packages.props .

# Restore packages
RUN dotnet restore BaGetter \
    --source "https://api.nuget.org/v3/index.json"

# Publish app (implicitly builds the app)
FROM build AS publish
RUN dotnet publish BaGetter \
    --configuration Release \
    --output /app \
    --no-restore

FROM base AS final
LABEL org.opencontainers.image.source="https://github.com/bagetter/BaGetter"
WORKDIR /app
COPY --from=publish /app .

# Use the /data folder for packages, symbols and the sqlite database
ENV Storage__Path "/data"
ENV Search__Type "Database"
ENV Database__Type "Sqlite"
ENV Database__ConnectionString "Data Source=/data/db/bagetter.db"
RUN mkdir -p "/data/packages"
RUN mkdir -p "/data/symbols"
RUN mkdir -p  "/data/db"

ENTRYPOINT ["dotnet", "BaGetter.dll"]
