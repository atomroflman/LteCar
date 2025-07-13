"use client";

import React, { useEffect, useState } from "react";
import GamepadViewer from "./gamepad-viewer";
import CarFunctionsView from "./car-functions-view";
import { useControlFlowStore } from "./control-flow-store";
import { useRouter } from "next/navigation";

export default function CarControl() {
  const [cars, setCars] = useState<{ id: string; driverId: string; driverName: string }[] | null>(null);
  const [carKey, setCarKey] = useState("");
  const [telemetrySubscribed, setTelemetrySubscribed] = useState(false);
  const flowControl = useControlFlowStore();
  const router = useRouter();

  // Load cars from backend
  useEffect(() => {
    fetch(`/api/car`)
      .then((res) => res.json())
      .then((data) => {
        setCars(data);
        if (data.length === 1)
          flowControl.setCarId(data[0].carId);
      });
  }, []);


  useEffect(() => {
    async function onCarSelected() {
      if (!flowControl.carId) 
        return;
      await flowControl.load(flowControl.carId as string);
      await flowControl.startConnection(flowControl.carId, undefined);
    }
    onCarSelected();
  }, [flowControl.carId]);

  // Start connection and session when carId and carKey are set
  const handleAquireCarControl = async () => {
    if (!flowControl.carId || !carKey) 
      return;
    await flowControl.startConnection(flowControl.carId, carKey);
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
            className="text-xs p-1 bg-red-500 text-white rounded w-full block"
            onClick={() => {
              flowControl.stopConnection();
              flowControl.setCarId(undefined);
              setCarKey("");
            }}
          >
            Stop Control
          </button>
          <label className="font-green-400 text-xs">
            Control Session aquired {flowControl.carId}
          </label>
        </>
      ) : (
        <>
          {cars && cars.length > 0 ?
            <>
              <select
                onChange={(e) => flowControl.setCarId(e.currentTarget.value)}
                value={flowControl.carId}
                className="text-xs p-1 w-full block"
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
              <button className="text-xs p-1 mt-1 border-1 w-full block" onClick={handleAquireCarControl}>
                Aquire Control
              </button>
            </> : <p className="text-xs text-red-500">No cars available. Please register a car first.</p>}
            
        </>
      )}
      {flowControl.carId &&
        <button onClick={() => router.push(`/car/${flowControl.carId}`)} className="text-xs text-blue-400 hover:underline w-full block mt-2">
          Control Flow Editor
        </button>}
      <GamepadViewer hideFlowButtons={true} />
      {flowControl.carId && <CarFunctionsView carId={flowControl.carId} hideFlowButtons={true} />}
    </div>
  );
}