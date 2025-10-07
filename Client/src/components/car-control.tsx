"use client";

import React, { useEffect, useState } from "react";
import GamepadViewer from "./gamepad-viewer";
import CarFunctionsView from "./car-functions-view";
import { useControlFlowStore } from "./control-flow-store";
import { useRouter } from "next/navigation";
import UpdateControl from "./update-control";
import SshKeyManager from "./ssh-key-manager";

export default function CarControl() {
  const [cars, setCars] = useState<{ id: string; driverId: string; driverName: string }[] | null>(null);
  const [telemetrySubscribed, setTelemetrySubscribed] = useState(false);
  const [sshPrivateKey, setSshPrivateKey] = useState("");
  const [showSshKeyInput, setShowSshKeyInput] = useState(false);
  const [controlMessage, setControlMessage] = useState<{ type: 'success' | 'error' | 'info'; text: string } | null>(null);
  const [isAcquiringControl, setIsAcquiringControl] = useState(false);
  const flowControl = useControlFlowStore();
  const router = useRouter();

  // Load cars from backend
  useEffect(() => {
    fetch(`/api/car`)
      .then((res) => res.json())
      .then((data) => {
        setCars(data);
        
        // Try to load last selected car from localStorage
        const lastCarId = localStorage.getItem('lastSelectedCarId');
        if (lastCarId && data.some((c: any) => c.id === lastCarId)) {
          flowControl.setCarId(lastCarId);
        } else if (data.length === 1) {
          // If only one car, select it automatically
          flowControl.setCarId(data[0].id);
          localStorage.setItem('lastSelectedCarId', data[0].id);
        }
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

      // Get challenge from vehicle via server
      console.log("Requesting challenge for carId:", flowControl.carId);
      const challenge = await connection.invoke("GetChallenge", flowControl.carId);
      console.log("Challenge received:", challenge);
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

      // Generate session ID from private key
      const sessionId = await flowControl.generateSessionIdFromPrivateKey(sshPrivateKey);
      if (!sessionId) {
        setControlMessage({ type: 'error', text: 'Failed to generate session ID' });
        setIsAcquiringControl(false);
        return;
      }

      // Send authentication to vehicle via server
      const sshAuth = `ssh:${challenge}|${signature}|${sessionId}`;
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
          <button
            className="text-xs p-1 bg-red-500 text-white rounded w-full block"
            onClick={() => {
              flowControl.stopConnection();
              flowControl.setCarId(undefined);
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
                onChange={(e) => {
                  const carId = e.currentTarget.value;
                  if (carId) {
                    flowControl.setCarId(carId);
                    localStorage.setItem('lastSelectedCarId', carId);
                  } else {
                    flowControl.setCarId(undefined);
                  }
                }}
                value={flowControl.carId || ""}
                className="text-xs p-2 w-full block border border-gray-300 rounded-md"
                style={{ minWidth: 0 }}
              >
                <option value="" className="text-xs">Please select a vehicle...</option>
                {cars?.map((c) => (
                  <option key={c.id} value={c.id} className="text-xs">
                    {c.id}
                  </option>
                ))}
              </select>
              
              {/* SSH Key Manager is shown below */}

              {/* Control Message */}
              {controlMessage && (
                <div className={`mt-2 p-3 rounded-md text-xs ${
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
                className={`text-xs p-2 mt-2 w-full block rounded-md font-medium transition-colors ${
                  isAcquiringControl 
                    ? 'bg-gray-400 text-gray-600 cursor-not-allowed' 
                    : 'bg-blue-600 text-white hover:bg-blue-700'
                }`} 
                onClick={handleAquireCarControl}
                disabled={isAcquiringControl}
              >
                {isAcquiringControl ? 'Acquiring Control...' : 'Acquire Control'}
              </button>
            </> : <p className="text-xs text-red-500">No cars available. Please register a car first.</p>}
            
        </>
      )}
      {flowControl.carId && !flowControl.carSession && (
        <>
          <SshKeyManager carId={flowControl.carId} />
        </>
      )}
      {flowControl.carId && flowControl.carSession && (
        <>
          <UpdateControl />
          <button onClick={() => router.push(`/car/${flowControl.carId}`)} className="text-xs text-blue-400 hover:underline w-full block mt-2">
            Control Flow Editor
          </button>
        </>
      )}
      <GamepadViewer hideFlowButtons={true} />
      {flowControl.carId && <CarFunctionsView carId={flowControl.carId} hideFlowButtons={true} />}
    </div>
  );
}