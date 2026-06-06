# syntax=docker/dockerfile:1

# 1) Build the Angular PWA. angular.json writes the bundle to ../Api/wwwroot,
#    so the output lands in /src/Api/wwwroot for the API publish stage to pick up.
FROM node:22 AS web-build
WORKDIR /src/Web
COPY src/Web/package*.json ./
RUN npm ci
COPY src/Web/ ./
RUN npm run build

# 2) Publish the API, including the Angular bundle copied into wwwroot.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src
COPY src/Api/ src/Api/
COPY --from=web-build /src/Api/wwwroot src/Api/wwwroot
RUN dotnet publish src/Api/RssReader.Api.csproj -c Release -o /app/publish

# 3) Runtime image — single deployable serving the API + PWA.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=api-build /app/publish ./
# SQLite file lives on a mounted volume so it survives container rebuilds.
ENV ConnectionStrings__Default="Data Source=/data/rssreader.db"
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RssReader.Api.dll"]
