# docker compose run --rm tests
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine
WORKDIR /src

COPY Directory.Build.props ./
COPY src/OrderFlow.Messaging/OrderFlow.Messaging.csproj src/OrderFlow.Messaging/
COPY src/OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj src/OrderFlow.Orders.Api/
COPY src/OrderFlow.Inventory.Worker/OrderFlow.Inventory.Worker.csproj src/OrderFlow.Inventory.Worker/
COPY tests/OrderFlow.Tests/OrderFlow.Tests.csproj tests/OrderFlow.Tests/
RUN dotnet restore tests/OrderFlow.Tests/OrderFlow.Tests.csproj

COPY . .
RUN dotnet build tests/OrderFlow.Tests/OrderFlow.Tests.csproj -c Release --no-restore

ENTRYPOINT ["dotnet", "test", "tests/OrderFlow.Tests/OrderFlow.Tests.csproj", "-c", "Release", "--no-build", "--logger", "console;verbosity=normal"]
