#!/bin/bash

mkdir janus
cd janus
set -e  # Stop on error

# 1. Update und notwendige Pakete installieren
sudo apt update
sudo apt install -y \
  git build-essential automake libtool \
  pkg-config gengetopt gtk-doc-tools \
  libmicrohttpd-dev libjansson-dev libnice-dev \
  libssl-dev libsrtp2-dev libsofia-sip-ua-dev \
  libglib2.0-dev libopus-dev libogg-dev \
  libini-config-dev libcollection-dev \
  cmake libusrsctp-dev

# 2. Quellcode klonen
git clone https://github.com/meetecho/janus-gateway.git
cd janus-gateway

# 3. Autogen und Konfiguration mit Custom-Options
./autogen.sh

./configure \
  --prefix=/opt/janus \
  --enable-websockets \
  --enable-rest \
  --disable-data-channels \
  --disable-mqtt \
  --disable-rabbitmq \
  --disable-docs \
  --disable-lua \
  --disable-plugin-nosip \
  --disable-plugin-sip \
  --disable-plugin-textroom \
  --disable-plugin-videocall \
  --disable-plugin-voicemail \
  --disable-plugin-echotest \
  --disable-plugin-videoroom \
  --disable-plugin-nanomsg \
  --disable-mqtt-event-handler \
  --disable-rabbitmq-event-handler

# AudioBridge bleibt aktiviert – für spätere Sprachfunktionen

# 4. Build und Installation
make -j$(nproc)
sudo make install
sudo make configs

# 5. Fertig
echo "✅ Janus erfolgreich gebaut und installiert!"
echo "👉 Starte mit: /opt/janus/bin/janus"

cd ..