# syntax=docker/dockerfile:1@sha256:ecfaec9ed6d810b56388c508f4121597bfbba70d41a6dfeee4d8cad5f295fc32

FROM --platform=$BUILDPLATFORM node:24-slim@sha256:0e0ff40c39bc087845bfb27465a0df4ea419520094bc35842ff83dd8cbe6f9b6 AS ui
RUN corepack enable && corepack prepare pnpm@latest --activate
WORKDIR /ui
COPY src/FunkArr.UI/package.json src/FunkArr.UI/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY src/FunkArr.UI/ .
RUN pnpm run build

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:3cc3bbbbf93d82104892f42aa9106b6be4d120346dea0649643a97c801525256 AS build
ARG TARGETARCH
ARG VERSION=0.0.0-dev
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
RUN dotnet publish FunkArr/FunkArr.csproj -c Release -a ${TARGETARCH} -o /app/publish /p:Version=${VERSION}

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:f62a272ac1b46e83f56b8ed0416572f31cd1128e2c4a5e63eb34d348e4a36095
# hadolint ignore=DL3018
RUN apk add --no-cache ffmpeg icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ARG VERSION=0.0.0-dev
LABEL org.opencontainers.image.title="funkarr" \
      org.opencontainers.image.description="German public broadcaster media libraries for the *arr ecosystem" \
      org.opencontainers.image.version="${VERSION}" \
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
