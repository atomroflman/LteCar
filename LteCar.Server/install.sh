#!/bin/bash

# Set current directory to the directory where the script is located
cd "$(dirname "$0")"
bash ./bash/install-janus.sh

curl -sSL https://dot.net/v1/dotnet-install.sh >> ./bash/dotnet-install.sh
bash ./bash/dotnet-install.sh

echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools' >> ~/.bashrc
source ~/.bashrc

apt install -y nodejs npm
# build client
cd ../Client
npm i
npm run build
cp -r ./out/* ../LteCar.Server/wwwroot

# build server
cd "$(dirname "$0")"
dotnet build -c=Release
cp -R ./wwwroot/* ./bin/Release/net8.0/wwwroot

SERVICE_NAME=LteCarServer
SCRIPT_PATH="$(dirname "$0")/../start-server.sh"
SERVICE_FILE=/etc/systemd/system/$SERVICE_NAME.service

echo "??? Erstelle systemd-Service: $SERVICE_NAME"

# Sicherstellen, dass das Script existiert
if [ ! -f "$SCRIPT_PATH" ]; then
    echo "? Fehler: $SCRIPT_PATH existiert nicht!"
    exit 1
fi

# Skript ausführbar machen
chmod +x "$SCRIPT_PATH"

# Service-Datei schreiben
sudo bash -c "cat > $SERVICE_FILE" <<EOF
[Unit]
Description=LteCar Server
After=network.target

[Service]
ExecStart=$SCRIPT_PATH
Restart=on-failure
User=root

[Install]
WantedBy=multi-user.target
EOF

# systemd neu laden und aktivieren
echo "?? Lade systemd neu und aktiviere den Service..."
sudo systemctl daemon-reload
sudo systemctl enable "$SERVICE_NAME"
sudo systemctl start "$SERVICE_NAME"

# Status anzeigen
sudo systemctl status "$SERVICE_NAME" --no-pager