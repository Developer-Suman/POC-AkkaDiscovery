# See https://aka.ms/customizecontainer to learn how to customize your debug container
# and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode
# (Default for Debug configuration)
FROM artifactory.cotiviti.com/docker/dotnet/runtime:10.0 AS base

USER $APP_UID
WORKDIR /app

# This stage is used to build the service project
FROM artifactory.cotiviti.com/docker/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["EditorService/EditorService.csproj", "EditorService/"]

RUN dotnet restore "./EditorService/EditorService.csproj"

COPY . .
WORKDIR "/src/EditorService"

RUN dotnet build "./EditorService.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore


# This stage is used to publish the service project
# to be copied to the final stage
FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "./EditorService.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore


# This stage is used in production or when running from VS
# in regular mode
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "EditorService.dll", "--console"]
