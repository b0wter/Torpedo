FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build-env
WORKDIR /app
COPY src/webapi/*.fsproj ./
RUN dotnet restore
COPY src/webapi/. ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal
# Configure torpedo here.
ENV TORPEDO_BASE_PATH /app/content/
ENV TORPEDO_UPLOADSENABLED false
WORKDIR /app/content
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "torpedo.dll"]
