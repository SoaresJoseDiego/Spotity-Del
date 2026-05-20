FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/SpotifyDel.slnx ./
COPY backend/src ./src

RUN dotnet restore SpotifyDel.slnx
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
