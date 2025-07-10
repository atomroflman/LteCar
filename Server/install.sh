#!/bin/bash

# Set current directory to the directory where the script is located
echo "changing directory to script location... ($(dirname "$0"))"
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
#cp -r ./out/* ../LteCar.Server/wwwroot

# build server
cd "$(dirname "$0")"
dotnet build -c=Release
cp -R ./wwwroot/* ./bin/Release/net8.0/wwwroot

read -p "Do you want to install the .NET and Node applications as services? (y/n): " install_service
if [[ "$install_service" =~ ^[Yy]$ ]]; then
    # .NET Service
    DOTNET_SERVICE_NAME=LteCarServer
    echo "Installing $DOTNET_SERVICE_NAME as a service..."
    echo "Dotnet executable: $(dirname "$0")/bin/Release/net8.0/LteCar.Server"
    DOTNET_SCRIPT_PATH="$(dirname "$0")/bin/Release/net8.0/LteCar.Server"
    DOTNET_SERVICE_FILE=/etc/systemd/system/$DOTNET_SERVICE_NAME.service

    echo "Creating systemd service: $DOTNET_SERVICE_NAME"
    if [ ! -f "$DOTNET_SCRIPT_PATH" ]; then
        echo "Error: $DOTNET_SCRIPT_PATH does not exist!"
        exit 1
    fi
    chmod +x "$DOTNET_SCRIPT_PATH"
    bash -c "cat > $DOTNET_SERVICE_FILE" <<EOF
[Unit]
Description=LteCar .NET Server
After=network.target

[Service]
ExecStart=/usr/bin/dotnet $DOTNET_SCRIPT_PATH
Restart=on-failure
User=root

[Install]
WantedBy=multi-user.target
EOF

    # Node Service
    NODE_SERVICE_NAME=LteCarClient
    NODE_SCRIPT_PATH="$(dirname \"$0\")/../Client/out"
    NODE_SERVICE_FILE=/etc/systemd/system/$NODE_SERVICE_NAME.service

    if [ ! -f "$NODE_SCRIPT_PATH" ]; then
        echo "Error: $NODE_SCRIPT_PATH does not exist!"
        exit 1
    fi
    chmod +x "$NODE_SCRIPT_PATH"
    bash -c "cat > $NODE_SERVICE_FILE" <<EOF
[Unit]
Description=LteCar Node Client
After=network.target

[Service]
ExecStart=/usr/bin/node $NODE_SCRIPT_PATH
Restart=on-failure
User=root

[Install]
WantedBy=multi-user.target
EOF

    # Reload systemd and enable/start both services
    echo "Reloading systemd and enabling services..."
    systemctl daemon-reload
    systemctl enable "$DOTNET_SERVICE_NAME"
    systemctl enable "$NODE_SERVICE_NAME"
    systemctl start "$DOTNET_SERVICE_NAME"
    systemctl start "$NODE_SERVICE_NAME"

    # Show status
    systemctl status "$DOTNET_SERVICE_NAME" --no-pager
    systemctl status "$NODE_SERVICE_NAME" --no-pager
else
    echo "Service installation skipped."
fi