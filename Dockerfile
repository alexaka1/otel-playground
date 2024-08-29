ARG BUILD_CONFIGURATION=Release
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION
WORKDIR /src
COPY ["OtelTester.csproj", "./"]
RUN dotnet restore "OtelTester.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "OtelTester.csproj" \
-c $BUILD_CONFIGURATION \
--no-restore \
-o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION
RUN dotnet publish "OtelTester.csproj" \
-c $BUILD_CONFIGURATION \
-o /app/publish \
--no-restore  \
/p:PublishAot=false \
/p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OtelTester.dll"]
