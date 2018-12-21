FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.fsproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/out .
COPY config_docker.json ./config.json
RUN mkdir wwwroot
RUN mkdir content
COPY wwwroot ./wwwroot
ENTRYPOINT ["dotnet", "torpedo.dll"]
