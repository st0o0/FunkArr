# syntax=docker/dockerfile:1@sha256:ecfaec9ed6d810b56388c508f4121597bfbba70d41a6dfeee4d8cad5f295fc32

FROM --platform=$BUILDPLATFORM node:22-slim@sha256:48e4b67d85f87bd551df43704e24d252f56cc5f8e9718841aace50f19948f0f9 AS ui
RUN corepack enable && corepack prepare pnpm@latest --activate
WORKDIR /ui
COPY src/FunkArr.UI/package.json src/FunkArr.UI/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY src/FunkArr.UI/ .
RUN pnpm run build

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:2fa828c68761b1b8c23d7662dc134421b9d3b59fe1425fdbc80804e390cdb24d AS build
ARG TARGETARCH
WORKDIR /src

COPY src/global.json src/Directory.Build.props src/Directory.Packages.props ./
COPY src/FunkArr/FunkArr.csproj FunkArr/
COPY src/FunkArr.Core/FunkArr.Core.csproj FunkArr.Core/
COPY src/FunkArr.Messages/FunkArr.Messages.csproj FunkArr.Messages/
COPY src/FunkArr.Persistence/FunkArr.Persistence.csproj FunkArr.Persistence/
COPY src/FunkArr.Api/FunkArr.Api.csproj FunkArr.Api/
COPY src/FunkArr.ArrApi/FunkArr.ArrApi.csproj FunkArr.ArrApi/
COPY src/FunkArr.Search/FunkArr.Search.csproj FunkArr.Search/
COPY src/FunkArr.Download/FunkArr.Download.csproj FunkArr.Download/
COPY src/FunkArr.RuleSet/FunkArr.RuleSet.csproj FunkArr.RuleSet/
COPY src/FunkArr.Scoring/FunkArr.Scoring.csproj FunkArr.Scoring/
COPY src/FunkArr.Enrichment/FunkArr.Enrichment.csproj FunkArr.Enrichment/
RUN dotnet restore FunkArr/FunkArr.csproj -a ${TARGETARCH}

COPY src/ .
COPY data/community/ruleset.schema.json /data/community/ruleset.schema.json
COPY --from=ui /ui/dist/ FunkArr/wwwroot/
RUN dotnet publish FunkArr/FunkArr.csproj -c Release -a ${TARGETARCH} -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble@sha256:6a94333d37514e385650a3c81a55e5350b67253dbe136e9cf17e499c35606a8c
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg && rm -rf /var/lib/apt/lists/*
LABEL org.opencontainers.image.title="funkarr" \
      org.opencontainers.image.description="German public broadcaster media libraries for the *arr ecosystem" \
      org.opencontainers.image.source="https://github.com/st0o0/funkarr" \
      org.opencontainers.image.documentation="https://github.com/st0o0/funkarr#readme"
RUN mkdir -p /app/data/temp && chown 1654:1654 /app/data /app/data/temp
WORKDIR /app
COPY --from=build /app/publish .
COPY data/community/rulesets/ /app/data/rulesets/community/
COPY data/community/version.txt /app/data/rulesets/version.txt
VOLUME /app/data
VOLUME /media
ENV ASPNETCORE_URLS=http://+:6969
EXPOSE 6969
ENTRYPOINT ["/bin/sh", "-c", "umask 000 && exec dotnet FunkArr.dll"]
