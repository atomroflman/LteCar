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
echo "You can run this setup again anytime with: ./setup-vehicle.sh"