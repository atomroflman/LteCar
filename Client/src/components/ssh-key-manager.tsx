"use client";

import React, { useState, useRef } from "react";
import { useControlFlowStore } from "./control-flow-store";
import CollapsibleSection from "./collapsible-section";

interface SshKeyManagerProps {
  carId: string;
}

export default function SshKeyManager({ carId }: SshKeyManagerProps) {
  const { downloadSshKey, uploadSshKey, getSshPrivateKey, removeSshPrivateKey } = useControlFlowStore();
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error' | 'info'; text: string } | null>(null);
  const [showUpload, setShowUpload] = useState(false);
  const [showDownload, setShowDownload] = useState(false);
  const [uploadKey, setUploadKey] = useState("");
  const [vehicleIp, setVehicleIp] = useState("");
  const [hasStoredKey, setHasStoredKey] = useState(getSshPrivateKey(carId) !== null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleDownloadKeyFromVehicle = async () => {
    if (!vehicleIp.trim()) {
      setMessage({ type: 'error', text: 'Please enter the vehicle IP address' });
      return;
    }

    setIsLoading(true);
    setMessage(null);
    
    try {
      const result = await downloadSshKey(carId, vehicleIp.trim(), false);
      if (result.success && result.key) {
        const saveSuccess = useControlFlowStore.getState().saveSshPrivateKey(carId, result.key);
        if (saveSuccess) {
          setMessage({ type: 'success', text: 'SSH key downloaded from vehicle and saved to browser!' });
          setHasStoredKey(true);
          setShowDownload(false);
          setVehicleIp("");
        } else {
          setMessage({ type: 'error', text: 'Failed to save SSH key to browser storage' });
        }
      } else {
        setMessage({ type: 'error', text: result.error || 'Failed to download SSH key from vehicle' });
      }
    } catch (error) {
      setMessage({ type: 'error', text: 'An unexpected error occurred' });
    } finally {
      setIsLoading(false);
    }
  };

  const handleDownloadKeyFile = () => {
    const key = getSshPrivateKey(carId);
    if (!key) {
      setMessage({ type: 'error', text: 'No SSH key found in browser storage' });
      return;
    }

    const blob = new Blob([key], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `vehicle-${carId}-key.pem`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    
    setMessage({ type: 'success', text: 'SSH key file downloaded successfully!' });
  };

  const handleUploadKey = async () => {
    if (!uploadKey.trim()) {
      setMessage({ type: 'error', text: 'Please enter a private key' });
      return;
    }

    setIsLoading(true);
    setMessage(null);
    
    try {
      const result = await uploadSshKey(carId, uploadKey.trim());
      if (result.success) {
        setMessage({ type: 'success', text: 'SSH key uploaded and saved successfully!' });
        setUploadKey("");
        setShowUpload(false);
        setHasStoredKey(true);
      } else {
        setMessage({ type: 'error', text: result.error || 'Failed to upload SSH key' });
      }
    } catch (error) {
      setMessage({ type: 'error', text: 'An unexpected error occurred' });
    } finally {
      setIsLoading(false);
    }
  };

  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => {
      const content = e.target?.result as string;
      setUploadKey(content);
    };
    reader.readAsText(file);
  };

  const handleRemoveKey = () => {
    if (confirm("Are you sure you want to remove the stored SSH key? This will prevent access to the vehicle.")) {
      const success = removeSshPrivateKey(carId);
      if (success) {
        setMessage({ type: 'info', text: 'SSH key removed successfully' });
        setHasStoredKey(false);
      } else {
        setMessage({ type: 'error', text: 'Failed to remove SSH key' });
      }
    }
  };

  const title = (
    <div className="flex items-center space-x-2">
      <div className={`w-1.5 h-1.5 rounded-full ${hasStoredKey ? 'bg-green-500' : 'bg-red-500'}`}></div>
      <span>SSH Key {hasStoredKey ? '(stored)' : '(no key)'}</span>
    </div>
  );

  return (
    <CollapsibleSection title={title as any} defaultCollapsed={true}>
      <div className="space-y-2">
          {/* Message */}
          {message && (
            <div className={`p-2 rounded text-xs ${
              message.type === 'success' ? 'bg-green-900/30 border border-green-700 text-green-300' :
              message.type === 'error' ? 'bg-red-900/30 border border-red-700 text-red-300' :
              'bg-blue-900/30 border border-blue-700 text-blue-300'
            }`}>
              {message.text}
            </div>
          )}

          {/* Show different options based on whether key is stored */}
          {!hasStoredKey ? (
            <>
              {/* Download from Vehicle */}
              <div>
                <button
                  onClick={() => setShowDownload(!showDownload)}
                  className="w-full text-left px-2 py-1.5 bg-zinc-800 hover:bg-zinc-700 rounded text-zinc-200 transition-colors"
                >
                  {showDownload ? '✕ Cancel' : '↓ Download from Vehicle'}
                </button>
                {showDownload && (
                  <div className="mt-1 p-2 bg-zinc-800 rounded space-y-1.5">
                    <input
                      type="text"
                      value={vehicleIp}
                      onChange={(e) => setVehicleIp(e.target.value)}
                      placeholder="192.168.1.100"
                      className="w-full px-2 py-1 bg-zinc-900 border border-zinc-700 rounded text-zinc-200 text-xs"
                    />
                    <button
                      onClick={handleDownloadKeyFromVehicle}
                      disabled={isLoading || !vehicleIp.trim()}
                      className="w-full px-2 py-1 bg-zinc-700 hover:bg-zinc-600 disabled:bg-zinc-800 disabled:text-zinc-600 rounded text-zinc-200 transition-colors"
                    >
                      {isLoading ? 'Downloading...' : 'Download'}
                    </button>
                  </div>
                )}
              </div>

              {/* Upload Key */}
              <div>
                <button
                  onClick={() => setShowUpload(!showUpload)}
                  className="w-full text-left px-2 py-1.5 bg-zinc-800 hover:bg-zinc-700 rounded text-zinc-200 transition-colors"
                >
                  {showUpload ? '✕ Cancel' : '↑ Upload Key'}
                </button>
                {showUpload && (
                  <div className="mt-1 p-2 bg-zinc-800 rounded space-y-1.5">
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept=".pem,.key,.txt"
                      onChange={handleFileUpload}
                      className="w-full text-xs text-zinc-400 file:mr-2 file:py-1 file:px-2 file:rounded file:border-0 file:text-xs file:bg-zinc-700 file:text-zinc-200 hover:file:bg-zinc-600"
                    />
                    <textarea
                      value={uploadKey}
                      onChange={(e) => setUploadKey(e.target.value)}
                      placeholder="-----BEGIN PRIVATE KEY-----"
                      className="w-full h-16 px-2 py-1 bg-zinc-900 border border-zinc-700 rounded text-zinc-200 text-xs font-mono"
                    />
                    <button
                      onClick={handleUploadKey}
                      disabled={isLoading || !uploadKey.trim()}
                      className="w-full px-2 py-1 bg-zinc-700 hover:bg-zinc-600 disabled:bg-zinc-800 disabled:text-zinc-600 rounded text-zinc-200 transition-colors"
                    >
                      {isLoading ? 'Uploading...' : 'Save Key'}
                    </button>
                  </div>
                )}
              </div>
            </>
          ) : (
            <>
              {/* Download Key File (for sharing) */}
              <button
                onClick={handleDownloadKeyFile}
                className="w-full text-left px-2 py-1.5 bg-zinc-800 hover:bg-zinc-700 rounded text-zinc-200 transition-colors"
              >
                💾 Download Key File
              </button>
              
              {/* Remove Key */}
              <button
                onClick={handleRemoveKey}
                className="w-full text-left px-2 py-1.5 bg-zinc-800 hover:bg-red-900/30 rounded text-red-400 transition-colors"
              >
                🗑️ Remove Key
              </button>
            </>
          )}
      </div>
    </CollapsibleSection>
  );
}