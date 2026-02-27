import React, { useEffect } from "react";
import { useTelemetryStore } from "./telemetry-store";

interface TelemetryProps {
  carId: number | undefined;
}

const Telemetry: React.FC<TelemetryProps> = ({ carId }) => {
  const availableChannels = useTelemetryStore((s) => s.availableChannels);
  const subscribedChannels = useTelemetryStore((s) => s.subscribedChannels);
  const entries = useTelemetryStore((s) => s.entries);
  const isConnected = useTelemetryStore((s) => s.isConnected);
  const connect = useTelemetryStore((s) => s.connect);
  const disconnect = useTelemetryStore((s) => s.disconnect);
  const subscribeChannel = useTelemetryStore((s) => s.subscribeChannel);
  const unsubscribeChannel = useTelemetryStore((s) => s.unsubscribeChannel);

  useEffect(() => {
    if (carId) {
      connect(carId);
    } else {
      disconnect();
    }
    return () => { disconnect(); };
  }, [carId]);

  if (!carId) {
    return (
      <div className="flex items-center justify-center h-full text-gray-500 dark:text-gray-400 text-sm">
        Kein Fahrzeug ausgewählt
      </div>
    );
  }

  const toggleChannel = (channelName: string) => {
    if (subscribedChannels.has(channelName)) {
      unsubscribeChannel(channelName);
    } else {
      subscribeChannel(channelName);
    }
  };

  return (
    <div className="flex items-center h-full px-4 gap-4 overflow-x-auto">
      {/* Connection status */}
      <div className="flex items-center gap-1.5 shrink-0">
        <span
          className={`inline-block w-2 h-2 rounded-full ${
            isConnected ? "bg-green-500" : "bg-red-500"
          }`}
        />
        <span className="text-xs text-gray-500 dark:text-gray-400">
          Telemetrie
        </span>
      </div>

      {/* Separator */}
      {availableChannels.length > 0 && (
        <div className="w-px h-5 bg-gray-300 dark:bg-gray-600 shrink-0" />
      )}

      {/* Channel toggles + values */}
      {availableChannels.map((ch) => {
        const isActive = subscribedChannels.has(ch.channelName);
        const entry = entries.find((e) => e.name === ch.channelName);

        return (
          <button
            key={ch.id}
            onClick={() => toggleChannel(ch.channelName)}
            className={`flex items-center gap-2 shrink-0 rounded px-3 py-1.5 text-left transition-colors ${
              isActive
                ? "bg-blue-100 dark:bg-blue-900/40 ring-1 ring-blue-300 dark:ring-blue-700"
                : "bg-gray-100 dark:bg-gray-800 opacity-60 hover:opacity-100"
            }`}
          >
            <span className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">
              {ch.channelName}
            </span>
            {isActive && entry && (
              <span className="text-sm font-mono font-semibold text-gray-900 dark:text-gray-100">
                {entry.value}
              </span>
            )}
            {isActive && !entry && (
              <span className="text-xs text-gray-400 dark:text-gray-500">…</span>
            )}
          </button>
        );
      })}

      {availableChannels.length === 0 && isConnected && (
        <span className="text-xs text-gray-400 dark:text-gray-500">
          Keine Telemetrie-Kanäle verfügbar
        </span>
      )}
    </div>
  );
};

export default Telemetry;
