"use client";

import React, { useEffect, useState } from "react";
import GamepadViewer from "./gamepad-viewer";
import CarFunctionsView from "./car-functions-view";
import { useControlFlowStore } from "./control-flow-store";

export default function CarControl({
  onShowUserSetupFlow,
}: {
  onShowUserSetupFlow?: () => void;
}) {
  const [cars, setCars] = useState<{ id: string; driverId: string; driverName: string }[] | null>(null);
  const [carKey, setCarKey] = useState("");
  const [selectedCarId, setSelectedCarId] = useState<string>("");
  const [telemetrySubscribed, setTelemetrySubscribed] = useState(false);
  const controlFlow = useControlFlowStore();

  // Load cars from backend
  useEffect(() => {
    fetch(`/api/car`)
      .then((res) => res.json())
      .then((data) => {
        setCars(data);
        if (data.length === 1) 
          setSelectedCarId(data[0].carId);
      });
  }, []);

  useEffect(() => { 
    controlFlow.startConnection(selectedCarId, undefined);
  }, [selectedCarId]);

  // Start connection and session when carId and carKey are set
  const handleAquireCarControl = async () => {
    if (!selectedCarId || !carKey) return;
    await controlFlow.startConnection(selectedCarId, carKey);
  };

  // Telemetry subscription (UI only, not part of control flow)
  function handleTelemetrySubscription(checked: boolean) {
    setTelemetrySubscribed(checked);
    // ...existing code for UI hub if needed...
  }

  return (
    <div className="p-2 space-y-2 text-xs leading-tight">
      <button
        className="mb-1 p-1 bg-gray-200 rounded w-full text-xs"
        onClick={onShowUserSetupFlow}
        type="button"
      >
        Steuerungs-Setup anzeigen
      </button>
      {controlFlow.carSession ? (
        <>
          <label className="font-green-400 text-xs">
            Control Session aquired: {controlFlow.carSession} - {controlFlow.carId}
          </label>
        </>
      ) : (
        <>
          <select
            onChange={(e) => setSelectedCarId(e.currentTarget.value)}
            value={selectedCarId}
            className="text-xs p-1"
            style={{ minWidth: 0 }}
          >
            {cars?.map((c) => (
              <option key={c.id} value={c.id} className="text-xs">
                {c.id}
              </option>
            ))}
          </select>
          <label htmlFor="textInput" className="block mb-1 font-medium text-xs">
            Car Key:
          </label>
          <input
            type="text"
            id="carKeyInput"
            value={carKey}
            onChange={(e) => setCarKey(e.target.value)}
            className="p-1 border rounded w-full text-xs"
          />
          <button className="text-xs p-1 mt-1" onClick={handleAquireCarControl}>Aquire Control</button>
        </>
      )}
      <GamepadViewer />
      {controlFlow.carId && <CarFunctionsView carId={controlFlow.carId} />}
    </div>
  );
}