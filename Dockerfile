FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/webapi/*.fsproj ./
RUN dotnet restore

# Copy everything else and build
COPY src/webapi/. ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
COPY src/webapi/config_docker.json ./config.json
ENTRYPOINT ["dotnet", "torpedo.dll"]
