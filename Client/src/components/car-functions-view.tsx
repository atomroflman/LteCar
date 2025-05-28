import React, { useEffect, useState } from "react";
import { useControlFlowStore } from "./control-flow-store";

export type CarFunction = {
  id: string;
  displayName: string;
  channelName: string;
  isEnabled: boolean;
  requiresAxis: boolean;
};

export default function CarFunctionsView({ carId, onRegisterOutput, hideFlowButtons }: { carId: string, onRegisterOutput?: (output: { channelName: string, displayName: string }) => void, hideFlowButtons?: boolean }) {
  const [functions, setFunctions] = useState<CarFunction[]>([]);
  const { nodes } = useControlFlowStore();

  useEffect(() => {
    if (!carId) return;
    fetch(`/api/car/${carId}/functions`)
      .then((res) => res.json())
      .then((data) => setFunctions(data));
  }, [carId]);

  // Only allow one output node per function
  const hasOutputNode = (channelName: string) =>
    nodes.some((n) => n.type === "output" && n.data?.channelName === channelName);

  return (
    <div className="bg-zinc-900 rounded-lg p-2 border border-zinc-800 text-xs mt-2">
      <div className="font-bold mb-2 text-zinc-200 text-xs">Fahrzeug-Funktionen</div>
      {functions.length === 0 && <div className="text-zinc-400 text-xs">Keine Funktionen gefunden.</div>}
      <ul className="space-y-1">
        {functions.map((f) => {
          const alreadyUsed = hasOutputNode(f.channelName);
          return (
            <li key={f.channelName} className="flex items-center justify-between">
              <div>
                <span className="font-mono text-zinc-100 text-xs">{f.displayName || f.channelName}</span>
                {f.requiresAxis && <span className="ml-2 text-zinc-400">(Achse)</span>}
                {!f.isEnabled && <span className="ml-2 text-red-400">(deaktiviert)</span>}
              </div>
              {!hideFlowButtons && onRegisterOutput && (
                <button
                  className="ml-2 px-1 py-0.5 bg-green-900 hover:bg-green-800 text-green-100 rounded text-[10px] border border-green-800 transition-colors duration-150 text-right whitespace-nowrap disabled:opacity-50"
                  disabled={alreadyUsed}
                  onClick={() => onRegisterOutput({ channelName: f.channelName, displayName: f.displayName })}
                >
                  +Flow
                </button>
              )}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
