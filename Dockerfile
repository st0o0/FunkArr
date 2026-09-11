# syntax=docker/dockerfile:1

# CI cross-compiles via `dotnet publish -r <rid>` and passes the
# published output as build context. No SDK needed here.

FROM mcr.microsoft.com/dotnet/aspnet:11.0-alpine
# hadolint ignore=DL3018
RUN apk add --no-cache ffmpeg
LABEL org.opencontainers.image.title="funkarr" \
      org.opencontainers.image.description="German public broadcaster media libraries for the *arr ecosystem" \
      org.opencontainers.image.source="https://github.com/st0o0/funkarr" \
      org.opencontainers.image.documentation="https://github.com/st0o0/funkarr#readme"
WORKDIR /app
COPY --chown=$APP_UID . .
RUN mkdir -p /app/data/temp && chown $APP_UID:$APP_UID /app/data /app/data/temp
VOLUME /app/data
VOLUME /media
ENV ASPNETCORE_URLS=http://+:6969
EXPOSE 6969
ENTRYPOINT ["dotnet", "FunkArr.dll"]
