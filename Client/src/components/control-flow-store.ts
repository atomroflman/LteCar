import { create } from 'zustand';
import { filterFunctionRegistry } from './filters/filter-function-registry';
import { Connection } from 'reactflow';

export type ControlFlowNode = {
  nodeId: number;
  representingId?: number;
  label: string;
  metadata: any;
  type: string;
  data: any;
  position: { x: number; y: number };
  nodeTypeName: string; // Name of Db Node Type für evaluation
  latestValue?: number | number[];
  params: Record<string, any>; // explizit als Objekt
  inputPorts?: number; // Anzahl Eingänge
  outputPorts?: number; // Anzahl Ausgänge
};

export type ControlFlowEdge = {
  id: number;
  source: number;
  sourcePort?: string;
  target: number;
  targetPort?: string;
};

export type ControlFlowState = {
  removeEdge(dbId: number): void;
  addEdge(params: Connection): void;
  nodeLatestValues: Record<string, number>;
  frame: number,
  nodes: ControlFlowNode[];
  edges: ControlFlowEdge[];
  isLoading: boolean;
  error: string | null;
  load: (carId: string) => Promise<void>;
  // save: (userId: string) => Promise<void>;
  setNodes: (nodes: ControlFlowNode[]) => void;
  setEdges: (edges: ControlFlowEdge[]) => void;
  updateNode: (node: ControlFlowNode) => void;
  reset: () => void;
  registerInput: (dbInputId: number ) => void;
  registerOutput: (dbOutputId: number) => void;
  registerFunctionNode: (fnName: string, params?: Record<string, any>, inputPorts?: number, outputPorts?: number) => void;
  deleteNode: (nodeId: number) => void;
  sendOutput: (channelId: number, value: number) => Promise<void>;
  startConnection: (carId: string, carKey: string | undefined) => Promise<void>;
  stopConnection: (carId: string, session: string | undefined) => Promise<void>;
  handleInputUpdate: (inputDbId: number, value: number) => void;
  connection: any;
  carId: string | undefined;
  userSetupId: number | undefined;
  setConnection: (connection: any) => void;
  setCarId: (carId: string) => void;
  setCarSession: (carSession: string) => void;
  carSession: string | undefined;
};

export const useControlFlowStore = create<ControlFlowState>((set, get) => ({
  nodeLatestValues: {},
  nodes: [],
  edges: [],
  frame: 0,
  isLoading: true,
  error: null,
  carId: undefined,
  userSetupId: undefined,
  async load(carId: string) {
    set({ isLoading: true, error: null });
    try {
      const res = await fetch(`/api/userconfig/setup/${carId}`);
      if (!res.ok) 
        throw new Error('Loading setup failed...');
      const data = await res.json() as {carId: number, id: number, userId: number};
      const flowRes = await fetch(`/api/flow/${data.id}`);
      if (!flowRes.ok)      
        throw new Error('Loading flow failed...');
      const flowData = await flowRes.json() as {nodes: ControlFlowNode[], edges: ControlFlowEdge[]};
      set({
        nodes: flowData.nodes || [],
        edges: flowData.edges || [],
        isLoading: false,
        error: null,
        carId,
        userSetupId: data.id,
      });
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
  async updateNode(node) {
    set(state => ({
      nodes: state.nodes.map(n => n.nodeId === node.nodeId ? { ...n, ...node, params: { ...node.params ?? n.params } } : n),
    }));
    if (node.params) {
      await fetch(`/api/flow/${node.nodeId}/params`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(node.params),
      });
    }
  },
  async addEdge(params: Connection) {
    const newEdgeRes = await fetch(`/api/flow/link`, {
      method: "POST",
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        fromNodeId: params.source,
        toNodeId: params.target,
        fromPort: params.sourceHandle,
        toPort: params.targetHandle,
      })
    });
    if (!newEdgeRes.ok) 
      throw new Error("Linking failed...");
    const newEdge = await newEdgeRes.json();
    set((state) => { 
      return {
        edges: [
          ...state.edges, 
          {
            id: newEdge.id,
            source: newEdge.source,
            target: newEdge.target,
            sourcePort: newEdge.sourcePort,
            targetPort: newEdge.targetPort,
          } as ControlFlowEdge
        ]
      };
    });
  },
  async removeEdge(dbId: number) {
    await fetch(`/api/flow/unlink/` + dbId, {
      method: "DELETE"
    });
    set(state => ({edges: state.edges.filter(e => e.id != dbId)}));
  },
  reset() {
    set({ nodes: [], edges: [], error: null });
  },
  async registerInput(dbId: number) {
    if (get().nodes.some(n => n.representingId == dbId && n.type == "input")) 
      return {};
    const res = await fetch(`/api/flow/input/${dbId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        userSetupId: get().userSetupId,
        id: dbId,
        positionX: 100,
        positionY: 100 + get().nodes.length * 40,
      })
    });
    if (!res.ok) {
      console.error("Failed to register input node", res.status, res.statusText);
      return {};
    }
    const input = await res.json();
    set(state => {      
      return {
        nodes: [
          ...state.nodes,
          {
            nodeId: input.nodeId,
            representingId: dbId,
            type: 'input',
            data: { ...input },
            position: { x: input.positionX, y: input.positionY },
            nodeTypeName: input.nodeTypeName,
            metadata: undefined,
            label: input.label,
            params: {}, // immer ein Objekt
          },
        ],
      };
    });
  },
  async registerOutput(dbId: number) {
    const nodeId = `output-${dbId}`;
    if (get().nodes.some(n => n.representingId == dbId && n.type == "output")) 
      return {};
    const res = await fetch(`/api/flow/output/${dbId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        userSetupId: get().userSetupId,
        id: dbId,
        positionX: 100,
        positionY: 400 + get().nodes.length * 40,
      })
    });
    if (!res.ok) {
      console.error("Failed to register input node", res.status, res.statusText);
      return {};
    }
    const output = await res.json();

    set(state => {     
      return {
        nodes: [
          ...state.nodes,
          {
            nodeId: output.nodeId,
            representingId: dbId,
            type: 'output',
            data: { ...output },
            position: { x: output.positionX, y: output.positionY },
            nodeTypeName: output.nodeTypeName,
            metadata: undefined,
            label: output.label,
            params: {}, // immer ein Objekt
          },
        ],
      }
    });
  },
  async registerFunctionNode(fnName: string, params: Record<string, any> = {}, inputPorts: number = 1, outputPorts: number = 1) {
    const res = await fetch(`/api/flow/function`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        userSetupId: get().userSetupId,
        SetupFunctionName: fnName,
        positionX: 100,
        positionY: 200 + get().nodes.length * 40,
        params,
        inputPorts,
        outputPorts,
      })
    });
    if (!res.ok) {
      console.error("Failed to register function node", res.status, res.statusText);
      return;
    }
    const fnNode = await res.json();
    set(state => ({
      nodes: [
        ...state.nodes,
        {
          nodeId: fnNode.nodeId,
          representingId: undefined,
          type: 'default',
          data: { ...fnNode },
          position: { x: fnNode.positionX, y: fnNode.positionY },
          nodeTypeName: fnNode.nodeTypeName,
          metadata: fnNode.metadata,
          label: fnNode.label,
          params: fnNode.params || params || {},
          inputPorts: fnNode.inputPorts || inputPorts,
          outputPorts: fnNode.outputPorts || outputPorts,
        },
      ],
    }));
  },
  async deleteNode(nodeId: number) {
    var res = await fetch(`/api/flow/${nodeId}`, {
      method: "DELETE"
    })
    if (!res.ok)
      throw new Error("Node deletion failed.");

    set(state => ({
      nodes: state.nodes.filter(n => n.nodeId !== nodeId),
      edges: state.edges.filter(e => e.source !== nodeId && e.target !== nodeId),
    }));
  },
  async sendOutput(channelId: number, value: number) {
    const { carId, carSession, connection } = get();
    console.log("send out", carId, carSession, connection?.state);
    if (connection && carId && carSession) {
      await connection.invoke("UpdateChannel", carId, carSession, channelId, value);
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
  async stopConnection(carId: string, session: string | undefined) {
    const { connection } = get();
    if (connection && carId && session) {
      await connection.invoke("ReleaseCarControl", carId, session)
      connection.stop();
      set({ connection: undefined, carId: undefined, carSession: undefined });
    } 
  },  
  handleInputUpdate(inputDbId: number, value: number) {
    const state = get();
    let queue: {nodeId: number, inputValues: Record<string, number>}[] = [];
    state.nodes.forEach(node => {
      if (node.type === "input" && node.representingId === inputDbId) {
        queue.push({ nodeId: node.nodeId, inputValues: { input: value } });
      }
    });
    const visited = new Set<number>();
    function executeNode(nodeId: number, inputValues: Record<string, number>): number[] {
      const node = state.nodes.find(n => n.nodeId === nodeId);
      if (!node) return [];
      if (node.nodeTypeName === "UserSetupUserChannelNode") {
        return [inputValues.input ?? 0];
      }
      if (node.nodeTypeName === "UserSetupFunctionNode") {
        const fnName: string | undefined = node.metadata?.functionName;
        if (!fnName) return [];
        const fnDef = filterFunctionRegistry[fnName as keyof typeof filterFunctionRegistry];
        if (!fnDef) return [];
        const mappedInputs: Record<string, number> = {};
        (fnDef.inputLabels as readonly string[]).forEach((label: string) => {
          let val = inputValues[label];
          if (val === undefined) {
            const incomingEdge = state.edges.find(e => e.target === node.nodeId && e.targetPort === label);
            if (incomingEdge) {
              const prevNode = state.nodes.find(n => n.nodeId === incomingEdge.source);
              if (prevNode) {
                const prevInputs: Record<string, number> = {};
                if (prevNode.type === "input") {
                  prevInputs.input = state.nodeLatestValues[prevNode.nodeId] ?? 0;
                } else {
                  const prevFnName: string | undefined = prevNode.metadata?.functionName;
                  if (prevFnName) {
                    const prevFn = filterFunctionRegistry[prevFnName as keyof typeof filterFunctionRegistry];
                    if (prevFn) {
                      (prevFn.inputLabels as readonly string[]).forEach((lab: string) => {
                        prevInputs[lab] = state.nodeLatestValues[prevNode.nodeId] ?? 0;
                      });
                    }
                  }
                }
                const prevOut = executeNode(prevNode.nodeId, prevInputs);
                val = prevOut[0];
              }
            }
          }
          mappedInputs[label] = val ?? 0;
        });
        return fnDef.apply(mappedInputs as any, node.params || {}, node.nodeId);
      }
      if (node.nodeTypeName === "UserSetupCarChannelNode") {
        state.sendOutput(node.representingId!, inputValues.input ?? 0);
        return [inputValues.input ?? 0];
      }
      return [];
    }
    while (queue.length > 0) {
      const next = queue.shift();
      if (!next?.nodeId) continue;
      if (visited.has(next.nodeId)) continue;
      visited.add(next.nodeId);
      const node = state.nodes.find(n => n.nodeId === next.nodeId);
      if (!node) continue;
      let calculatedValue: number[] = executeNode(node.nodeId, next.inputValues);
      if (calculatedValue[0] === state.nodeLatestValues[node.nodeId]) 
        continue;
      state.nodeLatestValues[node.nodeId] = calculatedValue[0];
      node.latestValue = calculatedValue[0];
      const nextEdges = state.edges.filter(e => e.source === node.nodeId);
      if (nextEdges.length === 0) 
        continue;
      nextEdges.forEach((edge, idx) => {
        const targetNode = state.nodes.find(n => n.nodeId === edge.target);
        if (!targetNode) 
          return;
        const targetFnName: string | undefined = targetNode.metadata?.functionName;
        const targetFn = targetFnName ? filterFunctionRegistry[targetFnName as keyof typeof filterFunctionRegistry] : undefined;
        const targetInputs: Record<string, number> = {};
        if (targetFn) {
          (targetFn.inputLabels as readonly string[]).forEach((label: string, i: number) => {
            const incoming = state.edges.find(e => e.target === targetNode.nodeId && e.targetPort === label);
            if (incoming && incoming.source === node.nodeId) {
              targetInputs[label] = calculatedValue[idx] ?? calculatedValue[0];
            } else {
              targetInputs[label] = state.nodeLatestValues[incoming?.source ?? 0] ?? 0;
            }
          });
        } else {
          targetInputs.input = calculatedValue[idx] ?? calculatedValue[0];
        }
        queue.push({ nodeId: targetNode.nodeId, inputValues: targetInputs });
      });
    }
    set({ nodeLatestValues: state.nodeLatestValues, nodes: [...state.nodes], frame: state.frame + 1 });
  },
  connection: undefined,
  carSession: undefined,
  setConnection: (connection: any) => set({ connection }),
  setCarId: (carId: string) => set({ carId }),
  setCarSession: (carSession: string) => set({ carSession }),
}));
