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

# Check if we are already inside a git repo
if [ -d ".git" ]; then
    echo "Already inside a git repository. Skipping clone."
else
    git clone https://github.com/atomroflman/LteCar.git
    if [ -n "$SUDO_USER" ]; then
        chown -R "$SUDO_USER":"$SUDO_USER" LteCar
    fi
fi