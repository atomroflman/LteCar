import React from "react";
import { Handle, Position, NodeProps } from "reactflow";
import { filterFunctionRegistry } from "./filters/filter-function-registry";
import { useControlFlowStore } from "./control-flow-store";
import { CustomFlowNodeProps } from "./custom-flow-node";
import { ParamInput } from "./param-input";

export default function FloatValueFlowNode(props: CustomFlowNodeProps) {
  // Umfassender Null-Check für props und props.data
  if (!props || !props.data) {
    return <div className="bg-zinc-800 border border-zinc-700 rounded p-2">Loading...</div>;
  }

  const id = props.data.nodeId;
  const flowControl = useControlFlowStore();
  
  // Verwende den globalen State direkt statt lokalen State
  const data = flowControl.nodes.find((n) => n.nodeId == id);

  if (!data?.metadata?.functionName || data.metadata.functionName !== 'FloatValue') {
    return null;
  }

  const definition = filterFunctionRegistry.FloatValue;
  const outputs = definition.outputLabels.map(e => e);
  const params = definition.params.map(p => ({ 
    name: p.name, 
    value: (data.params ?? {})[p.name] || p.default 
  }));

  // Get current value
  const outputValues = Array.isArray(data?.latestValue) ? data.latestValue : [data?.latestValue];
  const currentValue = outputValues[0] || 0;

  // Parse parameters
  const value = parseFloat(params.find(p => p.name === 'value')?.value || '0');
  const min = parseFloat(params.find(p => p.name === 'min')?.value || '-1');
  const max = parseFloat(params.find(p => p.name === 'max')?.value || '1');
  const step = parseFloat(params.find(p => p.name === 'step')?.value || '0.1');

  const outputHandles = outputs.map((label, index) => (
    <Handle
      key={`${label}`}
      type="source"
      position={Position.Bottom}
      id={`${label}`}
      className="bg-blue-500"
      style={{ left: `${100 / (outputs.length + 1) * (index + 1)}%`, bottom: "-4px" }}
    >  
      <span className="text-[8px] text-blue-300 font-mono block" style={{ marginTop: -24, marginLeft: -4 }}>
        {label}: {Number(currentValue).toFixed(3)}
      </span>
    </Handle>
  ));

  return (
    <div className="bg-zinc-800 border border-zinc-700 rounded p-2 pt-3 pb-6 flex flex-col min-w-[180px] relative">
      <div className="flex items-center justify-between mb-2">
        <span className="truncate text-xs font-mono font-bold text-cyan-300">🔢 Float Value</span>
        <button
          className="ml-2 px-1 py-0.5 bg-red-900 hover:bg-red-800 text-red-100 rounded text-[10px] border border-red-800 transition-colors duration-150"
          onClick={() => flowControl.deleteNode(Number(id))}
          title="Node löschen"
        >
          ✕
        </button>
      </div>

      {/* Current Value Display */}
      <div className="bg-zinc-900 border border-zinc-600 rounded p-2 mb-2 text-center">
        <div className="text-xs text-zinc-400 mb-1">Current Value</div>
        <div className="text-lg font-bold text-cyan-300">{Number(value).toFixed(3)}</div>
        <div className="text-xs text-zinc-500">Range: {min} - {max}</div>
      </div>

      {/* Value Controls */}
      <div className="mb-2">        
        {/* Button Grid Layout - 2 rows x 5 columns */}
        <div className="grid grid-cols-5 gap-1 mb-2">
          {/* First row: min | -step | -0.1 | -0.01 | -0.001 */}
          <button
            className="px-2 py-1 bg-red-800 hover:bg-red-700 text-red-200 rounded text-xs font-mono"
            onClick={(e) => {
              e.stopPropagation();
              props.data.params.value = min.toString();
              flowControl.updateNodeParams(props.data.nodeId, props.data.params);
            }}
            title="Set to minimum value"
          >
            min
          </button>
          <button
            className="px-2 py-1 bg-zinc-700 hover:bg-zinc-600 text-zinc-300 rounded text-xs font-mono"
            onClick={(e) => {
              e.stopPropagation();
              const newValue = Math.max(min, value - step);
              props.data.params.value = newValue.toString();
              flowControl.updateNodeParams(props.data.nodeId, props.data.params);
            }}
            title={`Decrease by ${step}`}
          >
            -{step}
          </button>
          <button
            className="px-2 py-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-400 rounded text-xs font-mono"
            onClick={(e) => {
              e.stopPropagation();
              const newValue = Math.max(min, value - 0.1);
              props.data.params.value = newValue.toString();
              flowControl.updateNodeParams(props.data.nodeId, props.data.params);
            }}
            title="Decrease by 0.1"
          >
            -0.1
          </button>
          <button
            className="px-2 py-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-400 rounded text-xs font-mono"
            onClick={(e) => {
              e.stopPropagation();
              const newValue = Math.max(min, value - 0.01);
              props.data.params.value = newValue.toString();
              flowControl.updateNodeParams(props.data.nodeId, props.data.params);
            }}
            title="Decrease by 0.01"
          >
            -0.01
          </button>
          <button
            className="px-2 py-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-400 rounded text-xs font-mono"
            onClick={(e) => {
              e.stopPropagation();
              const newValue = Math.max(min, value - 0.001);
              props.data.params.value = newValue.toString();
              flowControl.updateNodeParams(props.data.nodeId, props.data.params);
            }}
            title="Decrease by 0.001"
          >
            -0.001
          </button>
          
          {/* Second row: max | +step | +0.1 | +0.01 | +0.001 */}
          <button
            className="px-2 py-1 bg-green-800 hover:bg-green-700 text-green-200 rounded text-xs font-mono"
            onClick={(e) => {
              e.stopPropagation();
              props.data.params.value = max.toString();
              flowControl.updateNodeParams(props.data.nodeId, props.data.params);
            }}
            title="Set to maximum value"
          >
            max
          </button>
          <button
            className="px-2 py-1 bg-zinc-700 hover:bg-zinc-600 text-zinc-300 rounded text-xs font-mono"
            onClick={(e) => {
              e.stopPropagation();
              const newValue = Math.min(max, value + step);
              props.data.params.value = newValue.toString();
              flowControl.updateNodeParams(props.data.nodeId, props.data.params);
            }}
            title={`Increase by ${step}`}
          >
            +{step}
          </button>
          <button
            className="px-2 py-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-400 rounded text-xs font-mono"
            onClick={(e) => {
              e.stopPropagation();
              const newValue = Math.min(max, value + 0.1);
              props.data.params.value = newValue.toString();
              flowControl.updateNodeParams(props.data.nodeId, props.data.params);
            }}
            title="Increase by 0.1"
          >
            +0.1
          </button>
          <button
            className="px-2 py-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-400 rounded text-xs font-mono"
            onClick={(e) => {
              e.stopPropagation();
              const newValue = Math.min(max, value + 0.01);
              props.data.params.value = newValue.toString();
              flowControl.updateNodeParams(props.data.nodeId, props.data.params);
            }}
            title="Increase by 0.01"
          >
            +0.01
          </button>
          <button
            className="px-2 py-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-400 rounded text-xs font-mono"
            onClick={(e) => {
              e.stopPropagation();
              const newValue = Math.min(max, value + 0.001);
              props.data.params.value = newValue.toString();
              flowControl.updateNodeParams(props.data.nodeId, props.data.params);
            }}
            title="Increase by 0.001"
          >
            +0.001
          </button>
        </div>
        
        <div className="flex justify-between text-xs text-zinc-400 mt-1">
          <span>{min}</span>
          <span className="font-mono">Range</span>
          <span>{max}</span>
        </div>
      </div>

      {/* Parameters */}
      {params && (
        <div className="flex flex-col gap-1 mt-1">
          {params.map((e) => (
            <ParamInput
              key={e.name}
              name={e.name}
              value={e.value}
              onBlur={props.handleParamChange}
            />
          ))}
        </div>
      )} 
      {outputHandles}
    </div>
  );
}
