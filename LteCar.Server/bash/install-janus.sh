#!/bin/bash

mkdir janus
cd janus
set -e  # Stop on error

# 1. Update and install packages
apt update
apt install -y \
  tcpdump build-essential automake libtool \
  pkg-config gengetopt gtk-doc-tools \
  libmicrohttpd-dev libjansson-dev libnice-dev \
  libssl-dev libsrtp2-dev libsofia-sip-ua-dev \
  libglib2.0-dev libopus-dev libogg-dev \
  libini-config-dev libcollection-dev \
  cmake libusrsctp-dev libglib2.0-dev libssl-dev zlib1g-dev libconfig-dev libwebsockets-dev

# 2. Clone Janus
git clone https://github.com/meetecho/janus-gateway.git
cd janus-gateway

# 3. Custom options
./autogen.sh

./configure \
  --prefix=/opt/janus \
  --enable-websockets \
  --enable-plugin-streaming \
  --enable-plugin-audiobridge \
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
  --disable-plugin-nanomsg \
  --disable-mqtt-event-handler \
  --disable-rabbitmq-event-handler \
  --disable-plugin-videoroom 

# 4. Build und Installation
make -j$(nproc)
make install
make configs

# 5. Done
echo "✅ Janus built successful!"
echo "👉 Start Janus with: /opt/janus/bin/janus"

cd ..