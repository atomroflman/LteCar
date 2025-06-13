"use client";

import React, { useEffect, useState } from "react";
import GamepadViewer from "./gamepad-viewer";
import CarFunctionsView from "./car-functions-view";
import { useControlFlowStore } from "./control-flow-store";
import Link from "next/link";

export default function CarControl() {
  const [cars, setCars] = useState<{ id: string; driverId: string; driverName: string }[] | null>(null);
  const [carKey, setCarKey] = useState("");
  const [selectedCarId, setSelectedCarId] = useState<string>("");
  const [telemetrySubscribed, setTelemetrySubscribed] = useState(false);
  const flowControl = useControlFlowStore();

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
      flowControl.load(selectedCarId as string);
    }, [selectedCarId]);

  useEffect(() => { 
    flowControl.startConnection(selectedCarId, undefined);
  }, [selectedCarId]);

  // Start connection and session when carId and carKey are set
  const handleAquireCarControl = async () => {
    if (!selectedCarId || !carKey) return;
    await flowControl.startConnection(selectedCarId, carKey);
  };

  // Telemetry subscription (UI only, not part of control flow)
  function handleTelemetrySubscription(checked: boolean) {
    setTelemetrySubscribed(checked);
    // ...existing code for UI hub if needed...
  }

  return (
    <div className="p-2 space-y-2 text-xs leading-tight">
      {flowControl.carSession ? (
        <>
          <button
            className="text-xs p-1 bg-red-500 text-white rounded"
            onClick={() => {
              flowControl.stopConnection();
              setSelectedCarId("");
              setCarKey("");
            }}
          >
            Stop Control
          </button>
          <label className="font-green-400 text-xs">
            Control Session aquired: {flowControl.carSession} - {flowControl.carId}
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
      <GamepadViewer hideFlowButtons={true} />
      {flowControl.carId && <CarFunctionsView carId={flowControl.carId} hideFlowButtons={true} />}
      <Link href={`/car/${selectedCarId}`} className="text-xs text-blue-400 hover:underline">
        Control Flow Editor
      </Link>
    </div>
  );
}