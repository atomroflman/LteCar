import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  reactStrictMode: true,
  output: 'standalone',
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: 'http://localhost:5000/api/:path*', // Proxy API-Anfragen an den .NET Webservice
      },
      {
        source: '/hubs/:path*',
        destination: 'http://localhost:5000/hubs/:path*', // Proxy WebSocket/SingalR
      },
      {
        source: '/janus/:path*',
        destination: 'http://localhost:8088/janus/:path*',
      },
      {
        source: '/janus-ws/:path*',
        destination: 'http://localhost:8188/janus/:path*',
      },
    ];
  }
};

export default nextConfig;
