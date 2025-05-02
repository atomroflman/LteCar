#!/bin/bash

# This script installs the shared packages for a Raspberry Pi setup.
# It is required to run this script with root privileges.
# Check if the script is run as root
if [ "$EUID" -ne 0 ]; then
    echo "Please run as root"
    exit
fi

apt update
apt upgrade
apt install git nano -y

git clone https://github.com/atomroflman/LteCar.git