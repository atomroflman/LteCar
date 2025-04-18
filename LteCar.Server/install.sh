#!/bin/bash

# Set current directory to the directory where the script is located
cd "$(dirname "$0")"
bash ./bash/install-janus.sh

curl -sSL https://dot.net/v1/dotnet-install.sh >> ./bash/dotnet-install.sh
bash ./bash/dotnet-install.sh

echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools' >> ~/.bashrc
source ~/.bashrc