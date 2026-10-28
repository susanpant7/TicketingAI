FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/TicketingAI.Api/TicketingAI.Api.csproj src/TicketingAI.Api/
COPY src/TicketingAI.Data/TicketingAI.Data.csproj src/TicketingAI.Data/
RUN dotnet restore src/TicketingAI.Api/TicketingAI.Api.csproj

COPY src/ src/
RUN dotnet publish src/TicketingAI.Api/TicketingAI.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

RUN mkdir -p /app/data && chown -R $APP_UID /app/data
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketingAI.Api.dll"]
