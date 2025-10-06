git pull

SCRIPT_DIR="$(realpath "$(dirname "$0")")"
dotnet build -c=Release $SCRIPT_DIR/../src/LteCar.Server.csproj
systemctl restart LteCarServer.service
systemctl restart LteCarClient.service