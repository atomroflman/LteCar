import type { NextConfig } from "next";
import { webpack } from "next/dist/compiled/webpack/webpack";

const nextConfig: NextConfig = {
  /* config options here */
  reactStrictMode: true,
  output: 'export',
  allowedDevOrigins: ["http://ubuntu:5000", "http://ubuntu:8188", "192.168.3.149"]
};

export default nextConfig;
