
sudo apt install -y libcamera-apps gstreamer1.0-tools gstreamer1.0-plugins-base \
  gstreamer1.0-plugins-good gstreamer1.0-plugins-bad gstreamer1.0-plugins-ugly \
  gstreamer1.0-libav gstreamer1.0-rtsp gstreamer1.0-webrtc janus
sudo apt install -y \
  libmicrohttpd-dev libjansson-dev libssl-dev libsrtp2-dev \
  libsofia-sip-ua-dev libglib2.0-dev libopus-dev libogg-dev \
  libini-config-dev libcollection-dev libconfig-dev pkg-config \
  gengetopt libtool automake git cmake build-essential \
  libnice-dev libcurl4-openssl-dev liblua5.3-dev libnanomsg-dev \
  libwebsockets-dev libevent-dev
  
cd ~
git clone https://github.com/WiringPi/WiringPi.git
cd WiringPi
./build
 
 gpio -v
