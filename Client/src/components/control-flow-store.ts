import { create } from 'zustand';
import { useEffect } from "react";

export type ControlFlowNode = {
  id: string;
  type: string;
  data: any;
  position: { x: number; y: number };
};

export type ControlFlowEdge = {
  id: string;
  source: string;
  target: string;
  type?: string;
  data?: any;
};

export type ControlFlowState = {
  nodes: ControlFlowNode[];
  edges: ControlFlowEdge[];
  isLoading: boolean;
  error: string | null;
  load: (userId: string) => Promise<void>;
  save: (userId: string) => Promise<void>;
  setNodes: (nodes: ControlFlowNode[]) => void;
  setEdges: (edges: ControlFlowEdge[]) => void;
  updateNode: (node: ControlFlowNode) => void;
  updateEdge: (edge: ControlFlowEdge) => void;
  reset: () => void;
  registerInput: (input: { name: string; value: number; gamepadId: string }) => void;
  registerOutput: (output: { channelName: string; displayName: string }) => void;
  deleteNode: (nodeId: string) => void;
  sendOutput: (channelName: string, value: number) => Promise<void>;
  startConnection: (carId: string, carKey: string | undefined) => Promise<void>;
  handleInputUpdate: (event: { name: string; value: number; gamepadId: string }) => void;
  connection: any;
  carId: string | undefined;
  carSession: string | undefined;
  setConnection: (connection: any) => void;
  setCarId: (carId: string) => void;
  setCarSession: (carSession: string) => void;
};

export const useControlFlowStore = create<ControlFlowState>((set, get) => ({
  nodes: [],
  edges: [],
  isLoading: false,
  error: null,
  async load(userId: string) {
    set({ isLoading: true, error: null });
    try {
      const res = await fetch(`/api/user/setup?userId=${userId}`);
      if (!res.ok) throw new Error('Fehler beim Laden der Control Flows');
      const data = await res.json();
      set({
        nodes: data.nodes || [],
        edges: data.edges || [],
        isLoading: false,
        error: null,
      });
    } catch (e: any) {
      set({ isLoading: false, error: e.message });
    }
  },
  async save(userId: string) {
    set({ isLoading: true, error: null });
    try {
      const { nodes, edges } = get();
      const res = await fetch(`/api/user/setup?userId=${userId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ nodes, edges }),
      });
      if (!res.ok) throw new Error('Fehler beim Speichern der Control Flows');
      set({ isLoading: false, error: null });
    } catch (e: any) {
      set({ isLoading: false, error: e.message });
    }
  },
  setNodes(nodes) {
    set({ nodes });
  },
  setEdges(edges) {
    set({ edges });
  },
  updateNode(node) {
    set(state => ({
      nodes: state.nodes.map(n => n.id === node.id ? node : n),
    }));
  },
  updateEdge(edge) {
    set(state => ({
      edges: state.edges.map(e => e.id === edge.id ? edge : e),
    }));
  },
  reset() {
    set({ nodes: [], edges: [], error: null });
  },
  registerInput(input: { name: string; value: number; gamepadId: string }) {
    set(state => {
      const nodeId = `input-${input.gamepadId}-${input.name}`;
      if (state.nodes.some(n => n.id === nodeId)) return {};
      return {
        nodes: [
          ...state.nodes,
          {
            id: nodeId,
            type: 'input',
            data: { ...input },
            position: { x: 100, y: 100 + state.nodes.length * 40 },
          },
        ],
      };
    });
  },
  registerOutput(output: { channelName: string; displayName: string }) {
    set(state => {
      const nodeId = `output-${output.channelName}`;
      if (state.nodes.some(n => n.id === nodeId)) return {};
      return {
        nodes: [
          ...state.nodes,
          {
            id: nodeId,
            type: 'output',
            data: { ...output },
            position: { x: 400, y: 100 + state.nodes.length * 40 },
          },
        ],
      };
    });
  },
  deleteNode(nodeId: string) {
    set(state => ({
      nodes: state.nodes.filter(n => n.id !== nodeId),
      edges: state.edges.filter(e => e.source !== nodeId && e.target !== nodeId),
    }));
  },
  async sendOutput(channelName: string, value: number) {
    const { carId, carSession, connection } = get();
    if (connection && carId && carSession) {
      await connection.invoke("UpdateChannel", carId, carSession, channelName, value);
    }
  },
  async startConnection(carId: string, carKey: string | undefined) {
    const signalR = await import("@microsoft/signalr");
    var { connection } = get();
    if (!get().connection) {
        connection = new signalR.HubConnectionBuilder()
        .withUrl(`/hubs/control`)
        .withAutomaticReconnect()
        .build();
        await connection.start();
    }
    if (carKey == undefined) {
        set({ connection, carId, carSession: undefined });
    }
    const carSession = await connection.invoke("AquireCarControl", carId, carKey);
    set({ connection, carId, carSession });
  },
  handleInputUpdate(event: { name: string; value: number; gamepadId: string }) {
    const state = get();
    // 1. Input-Node suchen und Wert setzen
    state.nodes.forEach(node => {
      if (node.type === "input" && node.data?.gamepadId === event.gamepadId && node.data?.name === event.name) {
        node.data.value = event.value;
      }
    });
    // 2. Alle verbundenen Nodes traversieren (BFS)
    let queue = state.edges.filter(e => e.source.startsWith(`input-${event.gamepadId}-${event.name}`)).map(e => e.target);
    const visited = new Set();
    while (queue.length > 0) {
      const nodeId = queue.shift();
      if (!nodeId || visited.has(nodeId)) continue;
      visited.add(nodeId);
      const node = state.nodes.find(n => n.id === nodeId);
      if (!node) continue;
      // Funktionsnode: auswerten, ggf. Wert weitergeben
      if (node.type === "function" && typeof node.data?.fn === "function") {
        const inputVal = event.value;
        const result = node.data.fn(inputVal);
        if (result !== undefined) {
          // Weiter an alle nachfolgenden Edges
          queue.push(...state.edges.filter(e => e.source === nodeId).map(e => e.target));
          // Wert an nachfolgende Nodes weitergeben (als neues Event)
          state.nodes.forEach(n2 => {
            if (state.edges.some(e => e.source === nodeId && e.target === n2.id)) {
              if (n2.type === "output") {
                state.sendOutput(n2.data.channelName, result);
              } else {
                queue.push(n2.id);
              }
            }
          });
        }
      } else if (node.type === "output") {
        state.sendOutput(node.data.channelName, event.value);
      }
    }
  },
  connection: undefined,
  carId: undefined,
  carSession: undefined,
  setConnection: (connection: any) => set({ connection }),
  setCarId: (carId: string) => set({ carId }),
  setCarSession: (carSession: string) => set({ carSession }),
}));
