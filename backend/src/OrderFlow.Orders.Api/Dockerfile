# Contexto de build: ./backend
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# restore primero para aprovechar la caché de capas
COPY Directory.Build.props ./
COPY src/OrderFlow.Messaging/OrderFlow.Messaging.csproj src/OrderFlow.Messaging/
COPY src/OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj src/OrderFlow.Orders.Api/
RUN dotnet restore src/OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj

COPY src/OrderFlow.Messaging/ src/OrderFlow.Messaging/
COPY src/OrderFlow.Orders.Api/ src/OrderFlow.Orders.Api/
RUN dotnet publish src/OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderFlow.Orders.Api.dll"]
