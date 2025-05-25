#!/bin/bash
if [ "$EUID" -ne 0 ]
  then echo "Please run as root"
  exit
fi

if [ -f pi-install-base.sh ]; then 
    echo "Repository is already checked out!"
else
    apt install git
    git clone https://github.com/atomroflman/LteCar.git
    if [ -n "$SUDO_USER" ]; then
        chown -R "$SUDO_USER":"$SUDO_USER" LteCar
    fi
    cd LteCar
fi

bash pi-install-base.sh
bash ./LteCar.Server/install.sh