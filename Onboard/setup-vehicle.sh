#!/bin/bash

# LTE Car Vehicle Setup Script
# This script helps you set up your vehicle configuration

echo "🚗 LTE Car Vehicle Setup"
echo "========================"
echo ""

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is not installed. Please install .NET 8.0 or later."
    exit 1
fi

# Check if we're in the correct directory
if [ ! -f "LteCar.Onboard.csproj" ]; then
    echo "❌ Please run this script from the Onboard directory."
    exit 1
fi

echo "✅ Starting vehicle setup tool..."
echo ""

# Run the setup tool
dotnet run -- setup

echo ""
echo "Setup completed. Configuration files have been saved."

read -r -p "Soll der Onboard-Client als systemd-Service installiert werden? [y/N] " install_service
if [[ ${install_service,,} == "y" || ${install_service,,} == "j" ]]; then
    echo "📦 Erstelle Release-Build..."
    dotnet publish LteCar.Onboard.csproj -c Release

    SERVICE_NAME="ltecar-onboard.service"
    SERVICE_PATH="/etc/systemd/system/${SERVICE_NAME}"
    PUBLISH_DIR="$(pwd)/bin/Release/net8.0/publish"
    DOTNET_PATH="$(command -v dotnet)"
    CURRENT_USER="$(whoami)"

    if [ -z "${DOTNET_PATH}" ]; then
        echo "❌ dotnet wurde im PATH nicht gefunden."
        exit 1
    fi

    DLL_PATH="${PUBLISH_DIR}/LteCar.Onboard.dll"
    if [ ! -f "${DLL_PATH}" ]; then
        echo "❌ Die veröffentlichte DLL wurde nicht gefunden (${DLL_PATH})."
        echo "   Bitte prüfen Sie den Build und versuchen Sie es erneut."
        exit 1
    fi

    EXEC_START="${DOTNET_PATH} ${DLL_PATH}"

    LOG_DIR="/var/log/ltecar"
    sudo mkdir -p "${LOG_DIR}"
    sudo touch "${LOG_DIR}/onboard.log" "${LOG_DIR}/onboard.err"
    sudo chown "${CURRENT_USER}:${CURRENT_USER}" "${LOG_DIR}/onboard.log" "${LOG_DIR}/onboard.err"

    echo "🛠️  Installiere systemd-Service ${SERVICE_NAME}..."
    sudo tee "${SERVICE_PATH}" > /dev/null <<EOF
[Unit]
Description=LTE Car Onboard Client
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
WorkingDirectory=$(pwd)
StandardOutput=append:${LOG_DIR}/onboard.log
StandardError=append:${LOG_DIR}/onboard.err
ExecStart=${EXEC_START}
Restart=always
RestartSec=5
User=${CURRENT_USER}
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

    sudo systemctl daemon-reload
    sudo systemctl enable --now "${SERVICE_NAME}"
    echo "✅ Service ${SERVICE_NAME} wurde installiert und gestartet."
else
    echo "ℹ️  Service-Installation übersprungen."
fi

echo "You can run this setup again anytime with: ./setup-vehicle.sh"