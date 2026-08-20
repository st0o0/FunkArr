# syntax=docker/dockerfile:1

# CI cross-compiles via `dotnet publish -r <rid>` and passes the
# published output as build context. No SDK needed here.

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble AS prep
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*
RUN mkdir -p /data/temp && chown 1654:1654 /data /data/temp

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra
LABEL org.opencontainers.image.title="funkarr" \
      org.opencontainers.image.description="German public broadcaster media libraries for the *arr ecosystem" \
      org.opencontainers.image.source="https://github.com/st0o0/funkarr" \
      org.opencontainers.image.documentation="https://github.com/st0o0/funkarr#readme"
WORKDIR /app
COPY --from=prep /usr/bin/ffmpeg /usr/bin/ffmpeg
COPY --chown=$APP_UID . .
COPY --from=prep --chown=$APP_UID /data /app/data
COPY --chown=$APP_UID data/community/rulesets/ /app/data/rulesets/community/
VOLUME /app/data
VOLUME /media
EXPOSE 6969
ENTRYPOINT ["dotnet", "FunkArr.dll"]
