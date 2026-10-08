# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The local feed only exists on a developer machine; in the image the BuildingBlocks packages come from GitHub Packages.
COPY global.json nuget.config Directory.Build.props Directory.Packages.props .editorconfig ./
RUN dotnet nuget disable source local && dotnet nuget enable source github

COPY src/DiceRoller.Operative.Domain/DiceRoller.Operative.Domain.csproj src/DiceRoller.Operative.Domain/
COPY src/DiceRoller.Operative.Application/DiceRoller.Operative.Application.csproj src/DiceRoller.Operative.Application/
COPY src/DiceRoller.Operative.Infrastructure/DiceRoller.Operative.Infrastructure.csproj src/DiceRoller.Operative.Infrastructure/
COPY src/DiceRoller.Operative.Api/DiceRoller.Operative.Api.csproj src/DiceRoller.Operative.Api/

# The token is a BuildKit secret: it is only visible to this RUN and never written to a layer or to nuget.config.
RUN --mount=type=secret,id=nuget_token,env=NUGET_AUTH_TOKEN,required=true \
    NuGetPackageSourceCredentials_github="Username=docker;Password=${NUGET_AUTH_TOKEN}" \
    dotnet restore src/DiceRoller.Operative.Api/DiceRoller.Operative.Api.csproj

COPY src/ src/
RUN dotnet publish src/DiceRoller.Operative.Api/DiceRoller.Operative.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID

HEALTHCHECK --interval=10s --timeout=3s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "DiceRoller.Operative.Api.dll"]
