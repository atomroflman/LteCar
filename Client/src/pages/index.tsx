import React, { useEffect, useState } from "react";
import { Geist, Geist_Mono } from "next/font/google";
import CarVideoPanel from "@/components/car-video-panel";
import VideoSettingsControl from "@/components/video-settings";
import { useControlFlowStore } from "@/components/control-flow-store";
import CarControl from "@/components/car-control";
import Telemetry from "@/components/telemetry";
import Ping from "@/components/ping";
import SessionTransfer from "@/components/session-transfer";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export default function Home() {
  const [user, setUser] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const selectedCarId = useControlFlowStore(state => state.carId);
  const carSession = useControlFlowStore(state => state.carSession);

  useEffect(() => {
    fetch("/api/user/me")
      .then(res => res.json())
      .then(data => setUser(data))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">Loading...</div>
    );
  }
  if (!user) {
    return (
      <div className="flex items-center justify-center min-h-screen">Kein User angemeldet.</div>
    );
  }

  return (
      <div
          className={`${geistSans.className} ${geistMono.className} min-h-screen flex flex-col`}
      >
        <div className="flex flex-1">
          <div className="flex-1 flex items-center justify-center p-4">
            <CarVideoPanel carId={selectedCarId} />
          </div>

          <div className="w-64 border-l border-gray-300 flex flex-col">
            <CarControl />
            <SessionTransfer />
            <VideoSettingsControl carId={selectedCarId} canManageEnabled={Boolean(user?.loginName && selectedCarId && carSession)} />
            <Ping />
          </div>
        </div>

        <div className="h-14 border-t border-gray-200 dark:border-gray-700">
          <Telemetry carId={selectedCarId} />
        </div>
      </div>
  );
}
