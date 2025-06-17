import React from "react";
import { Handle, Position, NodeProps } from "reactflow";
import { filterFunctionRegistry } from "./filters/filter-function-registry";
import { useControlFlowStore } from "./control-flow-store";

export default function CustomFlowNode(props: NodeProps) {
  const id = props.data.nodeId;
  const flowControl = useControlFlowStore();
  const [data, setData] = React.useState<any>();

  React.useEffect(() => {
    setData(flowControl.nodes.find((n) => n.nodeId == id));
  }, [flowControl.frame, props.data.nodeId]);

  // Parameter-Änderung
  const handleParamChange = (key: string, value: any) => {
    if (!flowControl.nodes) 
      return;
    console.log(props.data.nodeId, key, value,  flowControl.nodes);
    const data = flowControl.nodes.find(n => n.nodeId == props.data.nodeId);
    if (!data) 
      return;
    const newParams = { ...data.params, [key]: value };
    const updatedNode = { ...data, params: newParams };
    flowControl.updateNodeParams(props.data.nodeId, newParams);
    const updatedNodes = flowControl.nodes.map(n => n.nodeId === props.data.nodeId ? updatedNode : n);
    setData(updatedNode);
    flowControl.setNodes(updatedNodes);
  };

  let inputs = [] as string[];
  let outputs = [] as string[];
  let params = [] as { name: string; value: string | number }[];
  if (data?.type === "input") {
    outputs.push("out");
  } else if (data?.type === "output") {
    inputs.push("in");
  } else {
    if (!data?.metadata?.functionName) {
      return (<>undefined!</>);
    }
    const definition = filterFunctionRegistry[data?.metadata?.functionName as keyof typeof filterFunctionRegistry];
    if (!definition) {
      return (<>Function not found!</>);
    }
    inputs = definition.inputLabels.map(e => e);
    outputs = definition.outputLabels.map(e => e);
    params = definition.params.map(p => ({ name: p.name, value: (data.params ?? [])[p.name] || p.default }));
    // for (let i = 0; i < (data?.outputPorts || 1); i++) {
    //   (outputHandles as React.ReactNode[]).push(
    //     <div key={"outwrap" + i} style={{ position: "absolute", left: `${10 + i * 30}px`, bottom: "-8px", display: "flex", flexDirection: "column", alignItems: "center" }}>
    //       <Handle type="source" position={Position.Bottom} id={"out" + i} />
    //       <span className="text-[10px] text-blue-300 font-mono" style={{ marginTop: 2 }}>
    //         {outputLabels[i] || `out${i}`}: {outputValues[i] !== undefined ? Number(outputValues[i]).toFixed(3) : ''}
    //       </span>
    //     </div>
    //   );
    // }
  }
  const outputValues = Array.isArray(data?.latestValue) ? data.latestValue : [data?.latestValue];
  const inputHandles = inputs.map((label, index) => (
    <Handle
      key={`${label}`}
      type="target"
      position={Position.Top}
      id={`${label}`}
      style={{ left: `${100 / (inputs.length + 1) * (index + 1)}%`, top: "-4px" }}
      className="bg-green-500"
    >
      <span className="text-[8px] pt-1 text-center text-green-300 font-mono block" style={{marginLeft: -50 / (inputs.length + 1), width: 100 / (inputs.length + 1)}}>{label}</span>
    </Handle>
  ));
  const outputHandles = outputs.map((label, index) => (
      <Handle
        type="source"
        position={Position.Bottom}
        id={`${label}`}
        className="bg-blue-500"
        style={{ left: `${100 / (outputs.length + 1) * (index + 1)}%`, bottom: "-4px" }}
      >  
        <span className="text-[8px] text-blue-300 font-mono block" style={{ marginTop: -24, marginLeft: -4 }}>
          {label}: {outputValues[index] !== undefined ? Number(outputValues[index]).toFixed(3) : ''}
        </span>
      </Handle>
  ));

  return (
    <div className="bg-zinc-800 border border-zinc-700 rounded p-2 pt-3 pb-6 flex flex-col min-w-[140px] relative">
      <div className="flex items-center justify-between">
        <span className="truncate text-xs font-mono">{data?.label}</span>
        <span className="truncate text-xs font-mono">
          {Array.isArray(data?.latestValue)
            ? data.latestValue.map((v: number) => Number(v).toFixed(3)).join(", ")
            : Number(data?.latestValue ?? '').toFixed(3)}
        </span>
        <button
          className="ml-2 px-1 py-0.5 bg-red-900 hover:bg-red-800 text-red-100 rounded text-[10px] border border-red-800 transition-colors duration-150"
          onClick={() => flowControl.deleteNode(Number(id))}
          title="Node löschen"
        >
          ✕
        </button>
      </div>
      
      {params && (
        <div className="flex flex-col gap-1 mt-1">
          {params.map((e) => (
            <div key={e.name} className="flex flex-row items-center justify-between gap-1">
              <span className="text-zinc-400 text-xs text-left flex-1">{e.name}:</span>
              <input
                className="bg-zinc-900 border border-zinc-700 rounded px-1 text-xs w-16 text-right"
                value={e.value}
                onChange={event => handleParamChange(e.name, event.target.value)}
              />
            </div>
          ))}
        </div>
      )} 

      {inputHandles}
      {outputHandles}
    </div>
  );
}
