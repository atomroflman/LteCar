import React, { useEffect, useRef } from "react";
import { useControlFlowStore } from "./control-flow-store";

type GamepadState = {
  id: string;
  axes: number[];
  buttons: number[];
};

// Unified event model for ReactFlow: { name: string, value: number, gamepadId: string }
export type GamepadUnifiedEvent = {
  name: string; // e.g., "axis-0", "button-1"
  value: number; // always -1 to 1
  gamepadId: string;
};

export default function GamepadViewer({ onUpdate, onRegisterInputChannelValue }: { onUpdate?: (event: GamepadUnifiedEvent) => void, onRegisterInputChannelValue?: (input: { name: string, value: number, gamepadId: string }) => void }) {
  const gamepadRef = useRef<Record<string, GamepadState>>({}); // gamepadId -> state
  const accuracyRef = useRef<number[]>([2, 2]);
  const [fps, setFps] = React.useState(15);
  const [gamepads, setGamepads] = React.useState<Record<string, GamepadState>>({});
  const [collapsed, setCollapsed] = React.useState(false);
  const controlFlow = useControlFlowStore();

  // Funktion: Unified Input Processing
  function handleInputUpdate(event: GamepadUnifiedEvent) {
    controlFlow.handleInputUpdate(event);
  }

  useEffect(() => {
    let lastGamepadIds: string[] = [];
    let interval: number;
    let running = true;
    const pollGamepad = () => {
      if (!running) return;
      const gps = Array.from(navigator.getGamepads()).filter(Boolean) as Gamepad[];
      const newGamepads: Record<string, GamepadState> = { ...gamepadRef.current };
      gps.forEach((gp) => {
        if (!gp) return;
        let old = gamepadRef.current[gp.id];
        const axes = gp.axes.map((ax, i) => {
          const acc = Math.pow(10, -accuracyRef.current[i] || 2);
          return Math.round(ax / acc) * acc;
        });
        const buttons = gp.buttons.map((b) => b.value);
        // New gamepad detected (send initial values)
        if (!lastGamepadIds.includes(gp.id)) {
          axes.forEach((a, i) => {
            handleInputUpdate({ name: `axis-${i}`, value: a, gamepadId: gp.id });
          });
          buttons.forEach((b, i) => {
            handleInputUpdate({ name: `button-${i}`, value: b, gamepadId: gp.id });
          });
        }
        if (old) {
          axes.forEach((a, i) => {
            if (a !== old.axes[i]) {
              handleInputUpdate({ name: `axis-${i}`, value: a, gamepadId: gp.id });
            }
          });
          buttons.forEach((b, i) => {
            if (b !== old.buttons[i]) {
              handleInputUpdate({ name: `button-${i}`, value: b, gamepadId: gp.id });
            }
          });
        }
        newGamepads[gp.id] = { id: gp.id, axes, buttons };
      });
      gamepadRef.current = newGamepads;
      setGamepads(newGamepads);
      lastGamepadIds = gps.map(gp => gp.id);
    };
    interval = window.setInterval(pollGamepad, 1000 / fps);
    return () => {
      running = false;
      clearInterval(interval);
    };
  }, [fps]);

  // UI: Sliders for accuracy, FPS, and gamepad status
  return (
    <div className={collapsed ? "mt-2 bg-zinc-900 text-zinc-100 rounded-lg p-0 text-xs leading-tight" : "mt-2 bg-zinc-900 text-zinc-100 rounded-lg p-2 border border-zinc-800 text-xs leading-tight"}
      style={!collapsed ? { maxHeight: 320, overflowY: 'auto' } : {}}>
      <button
        className="mb-2 px-2 py-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-200 rounded text-xs border border-zinc-700 transition-colors duration-150 w-full flex items-center justify-between"
        onClick={() => setCollapsed(c => !c)}
        aria-label={collapsed ? 'Expand Gamepad Viewer' : 'Collapse Gamepad Viewer'}
        style={collapsed ? { marginBottom: 0 } : {}}
      >
        <span>Gamepad Viewer</span>
        <span className="ml-2">{collapsed ? '▼' : '▲'}</span>
      </button>
      {!collapsed && (
        <>
          <div className="mb-1 font-semibold text-zinc-200 text-xs">Gamepad Accuracy:</div>
          {[0, 1].map((i) => {
            const accVal = accuracyRef.current[i] ?? 2;
            const decimals = accVal;
            const step = 1;
            const min = 0;
            const max = 8;
            const pow = Math.pow(10, -decimals);
            return (
              <div key={i} className="flex items-center mb-1">
                <span className="mr-1 text-zinc-300 text-xs">Axis {i}:</span>
                <input
                  type="range"
                  min={min}
                  max={max}
                  step={step}
                  defaultValue={decimals}
                  onChange={e => {
                    accuracyRef.current[i] = parseInt(e.target.value);
                    setFps(fps => fps); // force rerender
                  }}
                  className="mx-1 accent-blue-400 bg-zinc-800 h-2"
                  style={{ width: 80 }}
                />
                <span className="ml-1 text-zinc-400 text-xs">{`${decimals} Dezimal${decimals === 1 ? '' : 'en'} (${pow.toFixed(decimals)})`}</span>
              </div>
            );
          })}
          <div className="mt-2 mb-1 font-semibold text-zinc-200 text-xs">Poll Interval (FPS):</div>
          <div className="flex items-center mb-1">
            <input
              type="range"
              min={10}
              max={30}
              step={1}
              value={fps}
              onChange={e => setFps(Number(e.target.value))}
              className="mx-1 accent-blue-400 bg-zinc-800 h-2"
              style={{ width: 60 }}
            />
            <span className="ml-1 text-zinc-400 text-xs">{fps} Hz</span>
          </div>
          <div className="mt-3">
            <div className="font-bold mb-1 text-zinc-200 text-xs">Gamepads:</div>
            {Object.values(gamepads).length === 0 && <div className="text-zinc-400 text-xs">No gamepad connected.</div>}
            {Object.values(gamepads).map(gp => (
              <div key={gp.id} className="mb-2 border border-zinc-700 p-1 rounded bg-zinc-800 text-xs">
                <div className="font-mono text-[10px] mb-1 text-zinc-400">ID: {gp.id}</div>
                <div className="font-semibold text-zinc-300 text-xs">Axes:</div>
                <ul className="ml-2 mb-1">
                  {gp.axes.map((a, i) => {
                    const acc = Math.pow(10, -(accuracyRef.current[i] || 2));
                    const formatted = (Math.round(a / acc) * acc).toFixed(accuracyRef.current[i] || 2);
                    return (
                      <li key={i} className="flex items-center text-xs justify-between whitespace-nowrap">
                        <div className="flex items-center min-w-0">
                          <span className="w-14 text-zinc-400 text-xs text-left whitespace-nowrap">axis-{i}:</span>
                          <span className="font-mono w-10 text-zinc-100 text-xs text-left whitespace-pre">{a > 0 ? " " + formatted : formatted}</span>
                        </div>
                        <button
                          className="ml-2 px-1 py-0.5 bg-blue-900 hover:bg-blue-800 text-blue-100 rounded text-[10px] border border-blue-800 transition-colors duration-150 text-right whitespace-nowrap"
                          onClick={() => onRegisterInputChannelValue?.({ name: `axis-${i}`, value: a, gamepadId: gp.id })}
                        >
                          +Flow
                        </button>
                      </li>
                    );
                  })}
                </ul>
                <div className="font-semibold text-zinc-300 text-xs">Buttons:</div>
                <ul className="ml-2">
                  {gp.buttons.map((b, i) => {
                    const isPressed = b > 0.5;
                    return (
                      <li key={i} className="flex items-center text-xs justify-between whitespace-nowrap">
                        <div className="flex items-center min-w-0">
                          <span className="w-14 text-zinc-400 text-xs text-left whitespace-nowrap">button-{i}:</span>
                          <span
                            className={`inline-block w-4 h-4 rounded-full border transition-colors duration-150 ${isPressed ? "bg-zinc-100 border-zinc-400" : "bg-zinc-700 border-zinc-500"}`}
                            style={{ marginLeft: '1.5rem', marginRight: '1.5rem' }}
                            title={isPressed ? "Pressed" : "Not pressed"}
                          />
                        </div>
                        <button
                          className="ml-2 px-1 py-0.5 bg-blue-900 hover:bg-blue-800 text-blue-100 rounded text-[10px] border border-blue-800 transition-colors duration-150 text-right whitespace-nowrap"
                          onClick={() => onRegisterInputChannelValue?.({ name: `button-${i}`, value: b, gamepadId: gp.id })}
                        >
                          +Flow
                        </button>
                      </li>
                    );
                  })}
                </ul>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
