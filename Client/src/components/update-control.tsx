"use client";

import React from "react";
import { useControlFlowStore } from "./control-flow-store";

export default function UpdateControl() {
  const { updatesEnabled, isInConfigMode, setUpdatesEnabled, setConfigMode } = useControlFlowStore();

  return (
    <div className="p-4 bg-gray-100 rounded-lg">
      <h3 className="text-lg font-semibold mb-3">Update Control</h3>
      
      <div className="space-y-3">
        {/* Updates Toggle */}
        <div className="flex items-center justify-between">
          <label className="text-sm font-medium">Updates to Vehicle</label>
          <button
            onClick={() => setUpdatesEnabled(!updatesEnabled)}
            className={`px-3 py-1 rounded text-sm font-medium transition-colors ${
              updatesEnabled
                ? "bg-green-500 text-white hover:bg-green-600"
                : "bg-red-500 text-white hover:bg-red-600"
            }`}
          >
            {updatesEnabled ? "Enabled" : "Disabled"}
          </button>
        </div>

        {/* Config Mode Toggle */}
        <div className="flex items-center justify-between">
          <label className="text-sm font-medium">Configuration Mode</label>
          <button
            onClick={() => setConfigMode(!isInConfigMode)}
            className={`px-3 py-1 rounded text-sm font-medium transition-colors ${
              isInConfigMode
                ? "bg-blue-500 text-white hover:bg-blue-600"
                : "bg-gray-500 text-white hover:bg-gray-600"
            }`}
          >
            {isInConfigMode ? "Config Mode" : "Normal Mode"}
          </button>
        </div>

        {/* Status Info */}
        <div className="text-xs text-gray-600 mt-2">
          <p>
            <strong>Status:</strong> {
              !updatesEnabled && isInConfigMode ? "Updates disabled (config mode default)" :
              !updatesEnabled ? "Updates disabled manually" :
              isInConfigMode ? "Updates enabled (manual override)" :
              "Updates active"
            }
          </p>
          <p className="mt-1">
            <strong>Note:</strong> In configuration mode, updates are disabled by default but can be manually enabled for testing.
          </p>
        </div>
      </div>
    </div>
  );
}
