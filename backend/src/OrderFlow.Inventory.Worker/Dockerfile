# Contexto de build: ./backend
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/OrderFlow.Messaging/OrderFlow.Messaging.csproj src/OrderFlow.Messaging/
COPY src/OrderFlow.Inventory.Worker/OrderFlow.Inventory.Worker.csproj src/OrderFlow.Inventory.Worker/
RUN dotnet restore src/OrderFlow.Inventory.Worker/OrderFlow.Inventory.Worker.csproj

COPY src/OrderFlow.Messaging/ src/OrderFlow.Messaging/
COPY src/OrderFlow.Inventory.Worker/ src/OrderFlow.Inventory.Worker/
RUN dotnet publish src/OrderFlow.Inventory.Worker/OrderFlow.Inventory.Worker.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# runtime sin ASP.NET: el worker no expone HTTP
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "OrderFlow.Inventory.Worker.dll"]
