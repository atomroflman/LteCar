import Image from "next/image";
import { Geist, Geist_Mono } from "next/font/google";
import VideoStream from "@/components/video-stream";
import CarControl from "@/components/car-control";

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
      className={`${geistSans.className} ${geistMono.className} min-h-screen p-8 pb-20 font-[family-name:var(--font-geist-sans)]`}
    >
      <VideoStream key="video-stream" />
      {/* <CarControl /> */}
    </div>
  );
}
