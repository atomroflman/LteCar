import Image from "next/image";
import { Geist, Geist_Mono } from "next/font/google";
import VideoStream from "@/components/video-stream";
import CarControl from "@/components/car-control";
import Telemetry from "@/components/telemetry";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export default function Home() {
  return (
      <div
          className={`${geistSans.className} ${geistMono.className} min-h-screen flex flex-col`}
      >
        <div className="flex flex-1">
          {/* Video Stream centered */}
          <div className="flex-1 flex items-center justify-center">
            <VideoStream key="video-stream" />
          </div>

          {/* Car Control on the right */}
          <div className="w-64 border-l border-gray-300">
            <CarControl />
          </div>
        </div>

        <div className="h-20 border-t border-gray-300">
          <Telemetry />
        </div>
      </div>
  );
}
