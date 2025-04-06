#!/bin/bash
if [ "$EUID" -ne 0 ]
  then echo "Please run as root"
  exit
fi

if [! -f pi-install-base.sh]; then 
    apt install git
    git pull https://github.com/atomroflman/LteCar.git
    cd LteCar
fi

bash pi-install-base.sh
bash ./LteCar.Server/install.sh