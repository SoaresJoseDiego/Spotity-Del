FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/src ./src

# Restore + publish only the Api project; it transitively pulls Application,
# Domain and Infrastructure. The .slnx also references tests which aren't
# needed (or shipped) in production.
RUN dotnet restore src/SpotifyDel.Api/SpotifyDel.Api.csproj
RUN dotnet publish src/SpotifyDel.Api/SpotifyDel.Api.csproj \
    -c Release \
    -o /app \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "SpotifyDel.Api.dll"]
