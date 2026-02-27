import React, { useEffect } from "react";
import { useTelemetryStore } from "./telemetry-store";

interface TelemetryProps {
  carId: number | undefined;
}

const Telemetry: React.FC<TelemetryProps> = ({ carId }) => {
  const entries = useTelemetryStore((s) => s.entries);
  const isConnected = useTelemetryStore((s) => s.isConnected);
  const subscribe = useTelemetryStore((s) => s.subscribe);
  const unsubscribe = useTelemetryStore((s) => s.unsubscribe);

  useEffect(() => {
    if (carId) {
      subscribe(carId);
    } else {
      unsubscribe();
    }
    return () => { unsubscribe(); };
  }, [carId]);

  if (!carId) {
    return (
      <div className="flex items-center justify-center h-full text-gray-500 dark:text-gray-400 text-sm">
        Kein Fahrzeug ausgewählt
      </div>
    );
  }

  return (
    <div className="flex items-center h-full px-4 gap-6 overflow-x-auto">
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

      {entries.length === 0 && isConnected && (
        <span className="text-xs text-gray-400 dark:text-gray-500">
          Warte auf Daten...
        </span>
      )}

      {entries.map((item) => (
        <div
          key={item.name}
          className="flex items-center gap-2 shrink-0 bg-gray-100 dark:bg-gray-800 rounded px-3 py-1.5"
        >
          <span className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">
            {item.name}
          </span>
          <span className="text-sm font-mono font-semibold text-gray-900 dark:text-gray-100">
            {item.value}
          </span>
        </div>
      ))}
    </div>
  );
};

export default Telemetry;
