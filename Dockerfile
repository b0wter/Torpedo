FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
COPY src/webapi/*.fsproj ./
RUN ls -la /usr/share/dotnet/host/fxr
RUN dotnet restore
COPY src/webapi/. ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
COPY src/webapi/config_docker.json ./config.json
ENTRYPOINT ["dotnet", "torpedo.dll"]
