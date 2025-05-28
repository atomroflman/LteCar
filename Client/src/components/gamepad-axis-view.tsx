import React from "react";

export default function GamepadAxisView({
  gpId,
  index,
  value = 0,
  accuracy = 2,
  alreadyUsed = false,
  hideFlowButtons,
  onRegisterInputChannelValue
}: {
  gpId: string;
  index: number;
  value?: number;
  accuracy?: number;
  alreadyUsed?: boolean;
  hideFlowButtons?: boolean;
  onRegisterInputChannelValue?: (input: { name: string; value: number; gamepadId: string }) => void;
}) {
  const acc = Math.pow(10, -accuracy);
  const formatted = (Math.round(value / acc) * acc).toFixed(accuracy);
  const name = `axis-${index}`;
  return (
    <li className="flex items-center text-xs justify-between whitespace-nowrap">
      <div className="flex items-center min-w-0">
        <span className="w-14 text-zinc-400 text-xs text-left whitespace-nowrap">axis-{index}:</span>
        <span className="font-mono w-10 text-zinc-100 text-xs text-left whitespace-pre">{formatted}</span>
      </div>
      {!hideFlowButtons && (
        <button
          className="ml-2 px-1 py-0.5 bg-blue-900 hover:bg-blue-800 text-blue-100 rounded text-[10px] border border-blue-800 transition-colors duration-150 disabled:opacity-50"
          disabled={alreadyUsed}
          onClick={() => onRegisterInputChannelValue?.({ name, value, gamepadId: gpId })}
        >
          +Flow
        </button>
      )}
    </li>
  );
}
