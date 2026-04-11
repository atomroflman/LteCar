"use client";

import React, { useEffect, useState } from "react";
import GamepadViewer from "./gamepad-viewer";
import CarFunctionsView from "./car-functions-view";
import AudioChat from "./audio-chat";
import { useControlFlowStore } from "./control-flow-store";
import { useRouter } from "next/navigation";
import SshKeyManager from "./ssh-key-manager";
import { useCarUiStore } from "./car-ui-store";

export default function CarControl() {
  const [cars, setCars] = useState<{ id: number; name: string; lastSeen: string; isConnected: boolean }[] | null>(null);
  const [telemetrySubscribed, setTelemetrySubscribed] = useState(false);
  const [sshPrivateKey, setSshPrivateKey] = useState("");
  const [showSshKeyInput, setShowSshKeyInput] = useState(false);
  const [controlMessage, setControlMessage] = useState<{ type: 'success' | 'error' | 'info'; text: string } | null>(null);
  const [isAcquiringControl, setIsAcquiringControl] = useState(false);
  const flowControl = useControlFlowStore();
  const router = useRouter();
  const updatesEnabled = flowControl.updatesEnabled;
  const setUpdatesEnabled = flowControl.setUpdatesEnabled;
  const liveCarStates = useCarUiStore(state => state.states);
  const connectCarUi = useCarUiStore(state => state.connect);

  useEffect(() => {
    void connectCarUi();
  }, [connectCarUi]);

  const carsWithLiveStatus = cars?.map(car => ({
    ...car,
    isConnected: liveCarStates[car.id]?.isConnected ?? car.isConnected,
  })) ?? null;
  const selectedCar = carsWithLiveStatus?.find(car => car.id === flowControl.carId);

  // Status colors based on connection and updates
  const statusStyles = flowControl.carSession
    ? (updatesEnabled
        ? { bg: 'bg-green-900/30', border: 'border-green-700', text: 'text-green-300', dot: 'bg-green-500' }
        : { bg: 'bg-yellow-900/30', border: 'border-yellow-700', text: 'text-yellow-300', dot: 'bg-yellow-400' })
    : { bg: 'bg-red-900/30', border: 'border-red-700', text: 'text-red-300', dot: 'bg-red-500' };

  useEffect(() => {
    let cancelled = false;

    const loadCars = async () => {
      const response = await fetch(`/api/car`);
      const data = await response.json();
      if (cancelled) {
        return;
      }

      setCars(data);

      const lastCarIdStr = localStorage.getItem('lastSelectedCarId');
      if (lastCarIdStr) {
        const lastCarId = parseInt(lastCarIdStr);
        if (data.some((c: any) => c.id === lastCarId)) {
          flowControl.setCarId(lastCarId);
        }
      } else if (data.length === 1) {
        flowControl.setCarId(data[0].id);
        localStorage.setItem('lastSelectedCarId', data[0].id.toString());
      }
    };

    void loadCars();

    return () => {
      cancelled = true;
    };
  }, []);


  useEffect(() => {
    async function onCarSelected() {
      if (!flowControl.carId) 
        return;
      await flowControl.load(flowControl.carId);
      await flowControl.startConnection(flowControl.carId, undefined);
    }
    onCarSelected();
  }, [flowControl.carId]);

  // Load SSH private key when carId changes
  useEffect(() => {
    if (flowControl.carId) {
      const savedKey = flowControl.getSshPrivateKey(flowControl.carId);
      if (savedKey) {
        setSshPrivateKey(savedKey);
      }
    }
  }, [flowControl.carId]);

  // Start connection and session with SSH key authentication (via SignalR)
  const handleAquireCarControl = async () => {
    if (!flowControl.carId) {
      setControlMessage({ type: 'error', text: 'No car selected' });
      return;
    }
    
    if (!sshPrivateKey) {
      setControlMessage({ type: 'error', text: 'SSH private key is required. Please download or upload a key first.' });
      return;
    }
    
    setIsAcquiringControl(true);
    setControlMessage(null);
    
    try {
      // Authentication happens via SignalR through the server
      // The server forwards the challenge/response to the vehicle
      const { connection } = flowControl;
      if (!connection) {
        setControlMessage({ type: 'error', text: 'Not connected to server' });
        setIsAcquiringControl(false);
        return;
      }

      const challenge = await connection.invoke("GetChallenge", flowControl.carId);
      if (!challenge) {
        setControlMessage({ type: 'error', text: 'Failed to get authentication challenge from vehicle. Is the vehicle connected?' });
        setIsAcquiringControl(false);
        return;
      }

      // Sign the challenge with private key
      const signature = await flowControl.signWithPrivateKey(challenge, sshPrivateKey);
      if (!signature) {
        setControlMessage({ type: 'error', text: 'Failed to sign challenge with private key' });
        setIsAcquiringControl(false);
        return;
      }

      // Send authentication to vehicle via server
      const sshAuth = {
        Challenge: challenge,
        Signature: signature
      };
      console.log("Sending authentication to vehicle via server:", sshAuth, flowControl.carId);
      const carSession = await connection.invoke("AquireCarControl", flowControl.carId, sshAuth);
      
      if (carSession) {
        flowControl.setCarSession(carSession);
        flowControl.markCarAsAuthenticated(flowControl.carId);
        setControlMessage({ type: 'success', text: 'Successfully acquired car control! You can now control the vehicle.' });
      } else {
        setControlMessage({ type: 'error', text: 'SSH authentication failed. Please check your key and try again.' });
      }
    } catch (error) {
      console.error("Authentication error:", error);
      setControlMessage({ type: 'error', text: 'An unexpected error occurred while acquiring control.' });
    } finally {
      setIsAcquiringControl(false);
    }
  };

  const handleSshKeySave = () => {
    if (!flowControl.carId || !sshPrivateKey) return;
    const success = flowControl.saveSshPrivateKey(flowControl.carId, sshPrivateKey);
    if (success) {
      alert("SSH private key saved successfully");
      setShowSshKeyInput(false);
    } else {
      alert("Failed to save SSH private key");
    }
  };

  const handleSshKeyRemove = () => {
    if (!flowControl.carId) return;
    const success = flowControl.removeSshPrivateKey(flowControl.carId);
    if (success) {
      setSshPrivateKey("");
      alert("SSH private key removed");
    } else {
      alert("Failed to remove SSH private key");
    }
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
          <div className={`w-full p-2 rounded-md ${statusStyles.bg} border ${statusStyles.border} ${statusStyles.text}`}>
            <div className="flex items-center space-x-2 text-xs">
              <div className={`w-1.5 h-1.5 rounded-full ${statusStyles.dot}`} />
              <div className="font-medium">Control session active</div>
            </div>
            <div className="mt-1 text-[11px] opacity-80">
              Car ID: {flowControl.carId} · Session: {String(flowControl.carSession).slice(0, 8)}…
            </div>
          </div>
          <div className="mt-2 flex items-center space-x-1">
            <button
              className={`px-2 py-1 text-xs rounded ${updatesEnabled ? 'bg-amber-600 hover:bg-amber-700' : 'bg-green-600 hover:bg-green-700'} text-white`}
              title={updatesEnabled ? 'Pause updates' : 'Resume updates'}
              onClick={() => setUpdatesEnabled(!updatesEnabled)}
            >
              ⏸
            </button>
            <button
              className="px-2 py-1 text-xs rounded bg-red-600 hover:bg-red-700 text-white"
              title="Stop control session"
              onClick={() => {
                flowControl.stopConnection();
                flowControl.setCarId(undefined);
              }}
            >
              ■
            </button>
            <button
              className="ml-auto px-2 py-1 text-xs rounded bg-zinc-700 hover:bg-zinc-600 text-zinc-100"
              title="Open Flow Control Editor"
              onClick={() => router.push(`/car/${flowControl.carId}`)}
            >
              🧭
            </button>
          </div>
        </>
      ) : (
        <>
          {carsWithLiveStatus && carsWithLiveStatus.length > 0 ?
            <>
              <div className="space-y-2 border border-slate-200/80 bg-white/70 p-3 shadow-[0_16px_32px_rgba(15,23,42,0.06)] backdrop-blur-sm">
                <div>
                  <div className="text-[10px] font-semibold uppercase tracking-[0.24em] text-slate-500">Vehicle</div>
                  <div className="mt-1 text-sm font-semibold text-slate-900">Choose control target</div>
                </div>

                <select
                  onChange={(e) => {
                    const carIdStr = e.currentTarget.value;
                    if (carIdStr) {
                      const carId = parseInt(carIdStr);
                      flowControl.setCarId(carId);
                      localStorage.setItem('lastSelectedCarId', carIdStr);
                    } else {
                      flowControl.setCarId(undefined);
                    }
                  }}
                  value={flowControl.carId?.toString() || ""}
                  className="text-sm p-3 w-full block border border-slate-300 bg-[linear-gradient(180deg,rgba(255,255,255,0.96),rgba(241,245,249,0.95))] text-slate-900 shadow-inner outline-none transition-all hover:border-sky-400 focus:border-sky-500 focus:ring-4 focus:ring-sky-100"
                >
                  <option value="" className="text-sm text-gray-500">Select a vehicle...</option>
                  {carsWithLiveStatus.map((c) => (
                    <option key={c.id} value={c.id} className="text-sm py-2">
                      {c.name || `Car ${c.id}`} (ID: {c.id}) {c.isConnected ? '· online' : '· offline'}
                    </option>
                  ))}
                </select>
              </div>

              {selectedCar && (
                <div className={`mt-2 border px-3 py-2 text-xs ${selectedCar.isConnected ? 'border-green-200 bg-green-50 text-green-800' : 'border-amber-200 bg-amber-50 text-amber-800'}`}>
                  <div className="flex items-center gap-2">
                    <span className={`inline-block h-2 w-2 rounded-full ${selectedCar.isConnected ? 'bg-green-500' : 'bg-amber-500'}`} />
                    <span className="font-medium">{selectedCar.isConnected ? 'Fahrzeug online' : 'Fahrzeug offline'}</span>
                  </div>
                  {!selectedCar.isConnected && (
                    <div className="mt-1 text-[11px] opacity-80">
                      Das Fahrzeug ist aktuell nicht verbunden. Streams und Kontrolle werden automatisch wieder aktiv, sobald es sich neu verbindet.
                    </div>
                  )}
                </div>
              )}
              
              {/* SSH Key Manager is shown below */}

              {flowControl.carId && (
                <>
                  {/* Control Message */}
                  {controlMessage && (
                    <div className={`mt-2 p-3 text-xs ${
                      controlMessage.type === 'success' ? 'bg-green-50 border border-green-200 text-green-800' :
                      controlMessage.type === 'error' ? 'bg-red-50 border border-red-200 text-red-800' :
                      'bg-blue-50 border border-blue-200 text-blue-800'
                    }`}>
                      <div className="flex items-center">
                        <div className={`w-4 h-4 mr-2 ${
                          controlMessage.type === 'success' ? 'text-green-500' :
                          controlMessage.type === 'error' ? 'text-red-500' :
                          'text-blue-500'
                        }`}>
                          {controlMessage.type === 'success' ? '✓' : controlMessage.type === 'error' ? '✕' : 'ℹ'}
                        </div>
                        {controlMessage.text}
                      </div>
                    </div>
                  )}

                  <button 
                    className={`text-xs p-2 mt-2 w-full block font-medium transition-colors ${
                      isAcquiringControl 
                        ? 'bg-gray-400 text-gray-600 cursor-not-allowed' 
                        : 'bg-blue-600 text-white hover:bg-blue-700'
                    }`} 
                    onClick={handleAquireCarControl}
                    disabled={isAcquiringControl}
                  >
                    {isAcquiringControl ? 'Acquiring Control...' : 'Acquire Control'}
                  </button>
                </>
              )}
            </> : <p className="text-xs text-red-500">No cars available. Please register a car first.</p>}
            
        </>
      )}
      {flowControl.carId && !flowControl.carSession && (
        <>
          <SshKeyManager carId={flowControl.carId} />
        </>
      )}
      {flowControl.carId && (
        <>
          <GamepadViewer hideFlowButtons={true} />
          <CarFunctionsView carId={flowControl.carId} hideFlowButtons={true} />
          <AudioChat carId={flowControl.carId} />
        </>
      )}
    </div>
  );
}