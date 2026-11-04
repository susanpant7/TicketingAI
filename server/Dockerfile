# Built from the repository ROOT (docker build -f server/Dockerfile .), which is also Render's default build context.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY server/src/TicketingAI.Api/TicketingAI.Api.csproj src/TicketingAI.Api/
COPY server/src/TicketingAI.Data/TicketingAI.Data.csproj src/TicketingAI.Data/
RUN dotnet restore src/TicketingAI.Api/TicketingAI.Api.csproj

COPY server/src/ src/
RUN dotnet publish src/TicketingAI.Api/TicketingAI.Api.csproj -c Release -o /app --no-restore

# Litestream: copies the SQLite database to a bucket, so the data survives a host that wipes its disk (see start.sh).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS litestream
ARG TARGETARCH=amd64
ARG LITESTREAM_VERSION=0.3.13
RUN curl -fsSL "https://github.com/benbjohnson/litestream/releases/download/v${LITESTREAM_VERSION}/litestream-v${LITESTREAM_VERSION}-linux-${TARGETARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin litestream

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
COPY --from=litestream /usr/local/bin/litestream /usr/local/bin/litestream
COPY server/start.sh server/litestream.yml /app/

RUN mkdir -p /app/data && chown -R $APP_UID /app/data
USER $APP_UID

EXPOSE 8080
# start.sh runs the app, with Litestream around it when LITESTREAM_ENABLED=true.
ENTRYPOINT ["/app/start.sh"]
