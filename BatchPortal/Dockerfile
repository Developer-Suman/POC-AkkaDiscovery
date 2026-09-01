# Stage 1: Base runtime image
FROM artifactory.cotiviti.com/docker/dotnet/aspnet:10.0 AS base

USER $APP_UID
WORKDIR /app

EXPOSE 8080
EXPOSE 8081

# Stage 2: Build Angular client
FROM artifactory.cotiviti.com/docker/node:lts AS build-angular

ARG NPM_BUILD_COMMAND="build"

WORKDIR /src

COPY ["batchportal.client/package*.json", "./"]

RUN npm ci

COPY ["batchportal.client/.", "./"]

RUN npm run $NPM_BUILD_COMMAND

RUN rm -rf bin obj

# Stage 3: Build .NET server
FROM artifactory.cotiviti.com/docker/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["BatchPortal.Server/BatchPortal.Server.csproj", "BatchPortal.Server/"]

# Do not build the Angular client while building the .NET server
RUN dotnet restore "BatchPortal.Server/BatchPortal.Server.csproj" \
    /p:BuildClientProject=false

COPY . .

WORKDIR "/src/BatchPortal.Server"

# Important: same skip flag here
RUN dotnet build "BatchPortal.Server.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    /p:BuildClientProject=false


# Stage 4: Publish
FROM build AS publish

ARG BUILD_CONFIGURATION=Release

COPY --from=build-angular /src/dist/batchportal.client/browser/. ./wwwroot/

RUN dotnet publish "BatchPortal.Server.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:BuildClientProject=false


# Stage 5: Final production image
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "BatchPortal.Server.dll"]