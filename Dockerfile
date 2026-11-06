# syntax=docker/dockerfile:1@sha256:4edf897a3ffa55b89f906fc8cc78afdb3f1834cc9c7083565e611a8a7d5fe99e

FROM --platform=$BUILDPLATFORM node:26-slim@sha256:5adad64f9a1fca2d51972caa65983634c8a774eb300d72b20f7f2a0f60368072 AS ui
RUN npm install -g pnpm@12.15.0
WORKDIR /ui
COPY src/FunkArr.UI/package.json src/FunkArr.UI/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY src/FunkArr.UI/ .
RUN pnpm run build

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:5562fd626eec39f6f53218f9b7d7856ab7195d51d846568304fbe85071a3b6e6 AS build
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
RUN dotnet publish FunkArr/FunkArr.csproj -c Release -a ${TARGETARCH} -o /app/publish /p:Version=${VERSION} /p:ContinuousIntegrationBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:486297ae006e95ee2ee3112a2c03e3d26e41fbbc64b7a29e28e7a18cc36b12bb
# hadolint ignore=DL3018
RUN apk add --no-cache ffmpeg icu-libs su-exec
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_gcServer=0
ENV DOTNET_GCConserveMemory=9
ARG VERSION=0.0.0-dev
LABEL org.opencontainers.image.title="funkarr" \
      org.opencontainers.image.description="German public broadcaster media libraries for the *arr ecosystem" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.source="https://github.com/st0o0/funkarr" \
      org.opencontainers.image.documentation="https://github.com/st0o0/funkarr#readme"
RUN mkdir -p /app/data/temp
WORKDIR /app
COPY --from=build /app/publish .
COPY data/community/rulesets/ /app/data/rulesets/community/
COPY data/community/version.txt /app/data/rulesets/version.txt
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
VOLUME /app/data
VOLUME /media
ENV ASPNETCORE_URLS=http://+:6969
EXPOSE 6969
ENTRYPOINT ["/entrypoint.sh"]
