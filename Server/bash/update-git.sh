git pull

SCRIPT_DIR="$(realpath "$(dirname "$0")")"
dotnet build -c=Release $SCRIPT_DIR/../src/LteCar.Server.csproj
systemctl restart LteCarServer.service
systemctl restart LteCarClient.service
wait 5
systemctl status LteCarServer.service --no-pager -l
systemctl status LteCarClient.service --no-pager -l