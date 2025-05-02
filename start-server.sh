cd "$(dirname "$0")/LteCar.Server/bin/Release/net8.0"
echo "Starting LteCar Server"
echo "$(dirname "$0")/LteCar.Server/bin/Release/net8.0/LteCar.Server"
dotnet LteCar.Server.dll --urls=https://0.0.0.0:5000
