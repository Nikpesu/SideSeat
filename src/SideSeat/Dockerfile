FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/SideSeat/SideSeat.csproj", "src/SideSeat/"]
RUN dotnet restore "src/SideSeat/SideSeat.csproj"

COPY . .
RUN dotnet publish "src/SideSeat/SideSeat.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
ARG APP_VERSION=v0.14
LABEL org.opencontainers.image.title="SideSeat Web"
LABEL org.opencontainers.image.version="${APP_VERSION}"
LABEL com.sideseat.container.version="${APP_VERSION}"
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SideSeat.dll"]
