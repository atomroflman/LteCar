import React, { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/router";
import GamepadViewer from "../../../components/gamepad-viewer";
import CarFunctionsView from "../../../components/car-functions-view";
import ReactFlow, { MiniMap, Controls, Background } from "reactflow";
import "reactflow/dist/style.css";

type FlowNode = { id: number; type: string; positionX: number; positionY: number };
type FlowLink = { source: number; target: number };
type FlowData = { nodes: FlowNode[]; links: FlowLink[] };

async function fetchUserSetupId(carId: string | string[] | undefined): Promise<number | null> {
  if (!carId) return null;
  const res = await fetch(`/api/car/${carId}/setup`);
  if (!res.ok) return null;
  const data = await res.json();
  return data.id ?? null;
}

async function fetchFlow(userSetupId: number | null): Promise<FlowData> {
  if (!userSetupId) return { nodes: [], links: [] };
  const res = await fetch(`/api/flow/${userSetupId}`);
  if (!res.ok) return { nodes: [], links: [] };
  return await res.json();
}

async function registerInputNode({ userSetupId, name, value, gamepadId }: { userSetupId: number; name: string; value: number; gamepadId: string }) {
  await fetch("/api/flow/addnode", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      userSetupId,
      type: "userchannel",
      elementId: `${gamepadId}:${name}`,
      positionX: 100,
      positionY: 100,
    }),
  });
}

async function registerOutputNode({ userSetupId, channelName, displayName }: { userSetupId: number; channelName: string; displayName: string }) {
  
  await fetch("/api/flow/addnode", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      userSetupId,
      type: "carchannel",
      elementId: channelName,
      positionX: 400,
      positionY: 100,
    }),
  });
}

export default function CarControlFlowPage() {
  const router = useRouter();
  const [flow, setFlow] = useState<FlowData>({ nodes: [], links: [] });
  const [loading, setLoading] = useState(true);
  const [userSetupId, setUserSetupId] = useState<number | null>(null);

  useEffect(() => {
    fetchUserSetupId(router.query.carId).then(setUserSetupId);
  }, [router.query.carId]);

  useEffect(() => {
    fetchFlow(userSetupId).then((data) => {
      setFlow(data);
      setLoading(false);
    });
  }, [userSetupId]);

  const handleRegisterInput = useCallback(
    async ({ name, value, gamepadId }: { name: string; value: number; gamepadId: string }) => {
      if (!userSetupId) return;
      await registerInputNode({ userSetupId, name, value, gamepadId });
      setFlow(await fetchFlow(userSetupId));
    },
    [userSetupId]
  );

  const handleRegisterOutput = useCallback(
    async ({ channelName, displayName }: { channelName: string; displayName: string }) => {
      if (!userSetupId) return;
      await registerOutputNode({ userSetupId, channelName, displayName });
      setFlow(await fetchFlow(userSetupId));
    },
    [userSetupId]
  );

  const reactFlowNodes = flow.nodes.map((n) => ({
    id: n.id.toString(),
    type: n.type === "UserSetupUserChannelNode" ? "input" : n.type === "UserSetupCarChannelNode" ? "output" : "default",
    data: { label: n.type },
    position: { x: n.positionX ?? 100, y: n.positionY ?? 100 },
  }));
  const reactFlowEdges = flow.links.map((l, idx) => ({
    id: `e${l.source}-${l.target}`,
    source: l.source.toString(),
    target: l.target.toString(),
    animated: true,
  }));

  if (loading) return <div className="p-8 text-zinc-300">Lade Control Flow...</div>;

  return (
    <div className="flex flex-col md:flex-row gap-4 p-4 bg-zinc-950 min-h-screen">
      <div className="w-full md:w-1/4 space-y-4">
        <GamepadViewer onRegisterInputChannelValue={handleRegisterInput} hideFlowButtons={false} />
        <CarFunctionsView carId={router.query.carId as string} onRegisterOutput={handleRegisterOutput} hideFlowButtons={false} />
      </div>
      <div className="flex-1 bg-zinc-900 rounded-lg p-2 min-h-[600px]">
        <ReactFlow
          nodes={reactFlowNodes}
          edges={reactFlowEdges}
          fitView
        >
          <MiniMap />
          <Controls />
          <Background />
        </ReactFlow>
      </div>
    </div>
  );
}
