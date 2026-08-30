# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build Angular client
FROM node:lts AS build-angular
ARG NPM_BUILD_COMMAND="build"
WORKDIR /src
COPY ["BatchPortal/batchportal.client/package*.json", "./"]
RUN npm ci
COPY ["BatchPortal/batchportal.client/.", "./"]
RUN npm run $NPM_BUILD_COMMAND

# Stage 3: Build .NET server
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BatchPortal/BatchPortal.Server/BatchPortal.Server.csproj", "BatchPortal/BatchPortal.Server/"]
RUN dotnet restore "BatchPortal/BatchPortal.Server/BatchPortal.Server.csproj" /p:BuildClientProject=false
COPY . .
WORKDIR "/src/BatchPortal/BatchPortal.Server"
RUN dotnet build "BatchPortal.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:BuildClientProject=false

# Stage 4: Publish — inject Angular dist into wwwroot before publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
COPY --from=build-angular /src/dist/batchportal.client/browser/. ./wwwroot/
RUN dotnet publish "BatchPortal.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:BuildClientProject=false

# Stage 5: Final production image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BatchPortal.Server.dll"]
