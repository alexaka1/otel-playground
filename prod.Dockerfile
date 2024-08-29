ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot AS build
## TARGET* args are auto populated https://docs.docker.com/build/guide/multi-platform/#platform-build-arguments
ARG TARGETARCH
ARG TARGETOS
ARG BUILD_CONFIGURATION
WORKDIR /src
COPY ["OtelTester.csproj", "./"]
RUN dotnet restore "OtelTester.csproj" -a $TARGETARCH -r $TARGETOS-$TARGETARCH
COPY . .

FROM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION
RUN dotnet publish "OtelTester.csproj" \
-c $BUILD_CONFIGURATION  \
-a $TARGETARCH  \
--no-restore  \
-o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./OtelTester"]
