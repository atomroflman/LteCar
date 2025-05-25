import React, { useEffect, useState } from "react";
import ReactFlow, {
  MiniMap,
  Controls,
  Background,
  addEdge,
  useNodesState,
  useEdgesState,
  Connection,
  Edge,
  Node
} from "reactflow";
import "reactflow/dist/style.css";
import { filterFunctionRegistry } from "./filter-function-registry";

// Typen für UserSetup
export type UserSetupChannel = {
  id: number;
  name: string;
  isAxis: boolean;
  calibrationMin: number;
  calibrationMax: number;
};
export type UserSetupFilter = {
  id: number;
  name: string;
  setupFilterTypeId: number;
  parameters: string;
};
export type UserSetupLink = {
  id: number;
  channelSourceId?: number;
  filterSourceId?: number;
  filterTargetId?: number;
  vehicleFunctionTargetId?: number;
  type: string;
};
export type UserCarSetup = {
  id: number;
  carId: number;
  userId: number;
  carSecret: string;
  userSetupChannels: UserSetupChannel[];
  userSetupFilters: UserSetupFilter[];
  userSetupLinks: UserSetupLink[];
};

// Hilfsfunktion für Tastatur-Inputs
const defaultKeyboardInputs = [
  { id: "keyboard-up", label: "↑ Up" },
  { id: "keyboard-down", label: "↓ Down" },
  { id: "keyboard-left", label: "← Left" },
  { id: "keyboard-right", label: "→ Right" },
  { id: "keyboard-space", label: "Space" },
];

export default function UserSetupFlow(props: { onClose: () => void }) {
  const [userSetup, setUserSetup] = useState<UserCarSetup | null>(null);
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  const [filterTypes, setFilterTypes] = useState<any[]>([]);
  const [inputValues, setInputValues] = useState<Record<string, any>>({});

  // Daten vom UserConfigController laden
  useEffect(() => {
    fetch(`/api/userconfig/setup`)
      .then((res) => res.json())
      .then((data) => {
        setUserSetup(data);
        // Nodes und Edges aus Setup generieren
        console.log("User Setup Data:", data);
        const channelNodes = data.userSetupChannels.map((ch: UserSetupChannel) => ({
          id: `channel-${ch.id}`,
          type: "input",
          data: { label: ch.name || `Channel ${ch.id}` },
          position: { x: Math.random() * 200, y: Math.random() * 200 },
        }));
        const filterNodes = data.userSetupFilters.map((f: UserSetupFilter) => ({
          id: `filter-${f.id}`,
          type: "default",
          data: { label: f.name || `Filter ${f.id}` },
          position: { x: Math.random() * 200 + 250, y: Math.random() * 200 },
        }));
        const keyboardNodes = defaultKeyboardInputs.map((k, i) => ({
          id: k.id,
          type: "input",
          data: { label: k.label },
          position: { x: 10, y: 300 + i * 50 },
        }));
        // Ziel-Nodes (z.B. Fahrzeugfunktionen)
        const functionNodes = data.userSetupLinks
          .filter((l: UserSetupLink) => l.vehicleFunctionTargetId)
          .map((l: UserSetupLink) => ({
            id: `function-${l.vehicleFunctionTargetId}`,
            type: "output",
            data: { label: `Funktion ${l.vehicleFunctionTargetId}` },
            position: { x: 600, y: Math.random() * 300 },
          }));
        // Edges aus Links
        const edges = data.userSetupLinks.map((l: UserSetupLink) => {
          let source = l.channelSourceId
            ? `channel-${l.channelSourceId}`
            : l.filterSourceId
            ? `filter-${l.filterSourceId}`
            : undefined;
          let target = l.filterTargetId
            ? `filter-${l.filterTargetId}`
            : l.vehicleFunctionTargetId
            ? `function-${l.vehicleFunctionTargetId}`
            : undefined;
          if (!source || !target) return null;
          return {
            id: `edge-${l.id}`,
            source,
            target,
            animated: true,
          };
        }).filter(Boolean);
        setNodes([...channelNodes, ...filterNodes, ...keyboardNodes, ...functionNodes]);
        setEdges(edges);
      });
  }, []);

  // Filtertypen laden
  useEffect(() => {
    fetch("/api/userconfig/filtertypes")
      .then((res) => res.json())
      .then(setFilterTypes);
  }, []);

  // Hilfsfunktion: Filter-Parameter parsen
  function parseParams(paramStr: string) {
    try {
      return JSON.parse(paramStr);
    } catch {
      return {};
    }
  }

  // Flow-Auswertung: propagate values
  function evaluateFlow(inputValues: Record<string, any>) {
    // Mappe Nodes nach ID
    const nodeMap = Object.fromEntries(nodes.map((n) => [n.id, n]));
    // Mappe Filter nach ID
    const filterMap = Object.fromEntries((userSetup?.userSetupFilters||[]).map((f) => [f.id, f]));
    // Mappe Filtertypen nach ID
    const filterTypeMap = Object.fromEntries(filterTypes.map((ft) => [ft.id, ft]));
    // Werte, die an jedem Node anliegen
    const nodeValues: Record<string, any> = { ...inputValues };
    // Edges sortieren (primitive TopoSort)
    const sortedEdges = [...edges];
    // Propagiere Werte entlang der Kanten
    sortedEdges.forEach((edge) => {
      const sourceVal = nodeValues[edge.source];
      let targetVal = sourceVal;
      // Wenn Ziel ein Filter ist, führe Funktion aus
      if (edge.target.startsWith("filter-")) {
        const filterId = parseInt(edge.target.replace("filter-", ""));
        const filter = filterMap[filterId];
        if (filter) {
          const filterType = filterTypeMap[filter.setupFilterTypeId];
          const fn = filterFunctionRegistry[filterType?.typeName];
          if (fn) {
            targetVal = fn(sourceVal, parseParams(filter.parameters));
          }
        }
      }
      nodeValues[edge.target] = targetVal;
    });
    return nodeValues;
  }

  const nodeValues = evaluateFlow(inputValues);

  // Node-Outputs anzeigen
  function renderNodeOutputs(nodeId: string) {
    const val = nodeValues[nodeId];
    return val !== undefined ? (
      <div className="text-xs text-blue-700">Output: {JSON.stringify(val)}</div>
    ) : null;
  }

  // Node hinzufügen (z.B. für neue Controller-Inputs)
  function addKeyboardInput() {
    const newId = `keyboard-custom-${Date.now()}`;
    setNodes((nds) => [
      ...nds,
      {
        id: newId,
        type: "input",
        data: { label: "Custom Key" },
        position: { x: 10, y: 500 },
      },
    ]);
  }

  // Speichern-Funktion
  async function saveFlow() {
    // Hier: nodes, edges, userSetup serialisieren und an API senden
    await fetch("/api/userconfig/setup", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ nodes, edges, userSetup }),
    });
  }

  return (
    <div style={{ width: "100%", height: "600px" }}>
      <button
        className="absolute top-2 right-2 text-2xl font-bold text-gray-600 hover:text-red-500 z-10"
        onClick={props.onClose}
        aria-label="Schließen"
        type="button"
      >
        ×
      </button>
      <button onClick={addKeyboardInput} className="mb-2 p-2 bg-blue-200 rounded">Tastatur-Input hinzufügen</button>
      <button onClick={saveFlow} className="mb-2 p-2 bg-green-200 rounded">Flow speichern</button>
      <ReactFlow
        nodes={nodes.map((n) => ({
          ...n,
          data: {
            ...n.data,
            outputs: renderNodeOutputs(n.id),
          },
        }))}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={(params: Edge | Connection) => setEdges((eds) => addEdge(params, eds))}
        fitView
      >
        <MiniMap />
        <Controls />
        <Background />
      </ReactFlow>
    </div>
  );
}
