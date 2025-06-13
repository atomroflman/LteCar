import React from "react";
import { filterFunctionRegistry } from "./filters/filter-function-registry";
import { useControlFlowStore } from "./control-flow-store";

export default function FunctionNodesView({ hideFlowButtons }: { hideFlowButtons?: boolean }) {
  const controlFlow = useControlFlowStore();
  const functionNames = Object.keys(filterFunctionRegistry);
  const [params, setParams] = React.useState<Record<string, any>>({});
  const [inputPorts, setInputPorts] = React.useState<Record<string, number>>({});
  const [outputPorts, setOutputPorts] = React.useState<Record<string, number>>({});

  return (
    <div className="bg-zinc-900 rounded-lg p-2 border border-zinc-800 text-xs mt-2">
      <div className="font-bold mb-2 text-zinc-200 text-xs">Filter/Function Nodes</div>
      {functionNames.length === 0 && (
        <div className="text-zinc-400 text-xs">Keine Funktionen registriert.</div>
      )}
      <ul className="space-y-2">
        {functionNames.map((fn) => {
          return (
              <div className="flex items-center justify-between">
                <span className="font-mono text-zinc-100 text-xs">{fn}</span>
                {!hideFlowButtons && (
                  <button
                    className="ml-2 px-1 py-0.5 bg-blue-900 hover:bg-blue-800 text-blue-100 rounded text-[10px] border border-blue-800 transition-colors duration-150 text-right whitespace-nowrap disabled:opacity-50"
                    onClick={async () => {
                      await controlFlow.registerFunctionNode(
                        fn,
                        params[fn] || {},
                        inputPorts[fn] || 1,
                        outputPorts[fn] || 1
                      );
                    }}
                  >
                    +Flow
                  </button>
                )}
              </div>
          );
        })}
      </ul>
    </div>
  );
}
