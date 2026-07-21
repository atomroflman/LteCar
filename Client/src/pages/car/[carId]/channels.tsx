import React, { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/router";
import Link from "next/link";
import CollapsibleSection from "@/components/collapsible-section";
import LanguageSwitcher from "@/components/language-switcher";
import ConfigGuard from "@/components/config-guard";
import { useI18n } from "@/i18n/provider";

type ControlChannel = {
  channelName: string;
  displayName?: string | null;
  isEnabled: boolean;
  requiresAxis: boolean;
  maxResendInterval?: number | null;
  controlType: string;
  pinManager: string;
  address?: number | null;
  testDisabled: boolean;
  optionsJson: string;
  modifiedAt?: string | null;
};

type TelemetryChannel = {
  channelName: string;
  readIntervalTicks: number;
  telemetryType: string;
  dataType: number;
  unit?: string | null;
  decimals?: number | null;
  modifiedAt?: string | null;
};

type VideoStream = {
  streamId: string;
  name?: string | null;
  type?: string | null;
  location?: string | null;
  enabled: boolean;
  width?: number | null;
  height?: number | null;
  framerate?: number | null;
  bitrateKbps?: number | null;
  modifiedAt?: string | null;
};

const DATA_TYPES = ["String", "Integer", "Float", "Boolean"];

function Field({ label, children, hint }: { label: string; children: React.ReactNode; hint?: string }) {
  return (
    <label className="flex flex-col gap-1 text-xs text-zinc-300">
      <span className="text-zinc-400">{label}</span>
      {children}
      {hint && <span className="text-zinc-500 text-[10px]">{hint}</span>}
    </label>
  );
}

const inputClass = "bg-zinc-800 text-zinc-100 border border-zinc-700 rounded px-2 py-1 text-xs";
const btnPrimary = "px-2 py-1 bg-green-900 hover:bg-green-800 text-green-100 rounded text-xs border border-green-800";
const btnDanger = "px-2 py-1 bg-red-900 hover:bg-red-800 text-red-100 rounded text-xs border border-red-800";
const btnGhost = "px-2 py-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-200 rounded text-xs border border-zinc-700";

function ControlEditor({
  initial, isNew, onSave, onCancel, controlTypes,
}: { initial?: ControlChannel; isNew: boolean; onSave: (c: ControlChannel) => Promise<void>; onCancel: () => void; controlTypes: string[] }) {
  const [c, setC] = useState<ControlChannel>(initial ?? {
    channelName: "", displayName: "", isEnabled: true, requiresAxis: false, maxResendInterval: null,
    controlType: "", pinManager: "default", address: null, testDisabled: false, optionsJson: "",
  });
  const update = (patch: Partial<ControlChannel>) => setC(prev => ({ ...prev, ...patch }));
  return (
    <div className="grid grid-cols-2 gap-2 p-2 border border-zinc-700 rounded bg-zinc-950">
      <Field label="Channel name"><input className={inputClass} value={c.channelName} onChange={e => update({ channelName: e.target.value })} disabled={!isNew} /></Field>
      <Field label="Display name"><input className={inputClass} value={c.displayName ?? ""} onChange={e => update({ displayName: e.target.value })} /></Field>
      <Field label="Control type">
        <input className={inputClass} value={c.controlType} onChange={e => update({ controlType: e.target.value })} list="control-type-options" placeholder="e.g. Steering, PwmLight" />
      </Field>
      <Field label="Pin manager"><input className={inputClass} value={c.pinManager} onChange={e => update({ pinManager: e.target.value })} /></Field>
      <Field label="Address (int)"><input className={inputClass} type="number" value={c.address ?? ""} onChange={e => update({ address: e.target.value === "" ? null : Number(e.target.value) })} /></Field>
      <Field label="Max resend interval (ms)"><input className={inputClass} type="number" value={c.maxResendInterval ?? ""} onChange={e => update({ maxResendInterval: e.target.value === "" ? null : Number(e.target.value) })} /></Field>
      <Field label="Enabled"><input type="checkbox" checked={c.isEnabled} onChange={e => update({ isEnabled: e.target.checked })} /></Field>
      <Field label="Requires axis"><input type="checkbox" checked={c.requiresAxis} onChange={e => update({ requiresAxis: e.target.checked })} /></Field>
      <Field label="Test disabled"><input type="checkbox" checked={c.testDisabled} onChange={e => update({ testDisabled: e.target.checked })} /></Field>
      <Field label="Options (JSON)" hint='e.g. {"blinkCycleMs":500}'>
        <textarea className={inputClass + " font-mono"} rows={2} value={c.optionsJson} onChange={e => update({ optionsJson: e.target.value })} />
      </Field>
      <div className="col-span-2 flex gap-2 justify-end">
        <button className={btnGhost} onClick={onCancel}>Cancel</button>
        <button className={btnPrimary} onClick={() => onSave(c)}>Save</button>
      </div>
    </div>
  );
}

function TelemetryEditor({
  initial, isNew, onSave, onCancel, telemetryTypes,
}: { initial?: TelemetryChannel; isNew: boolean; onSave: (t: TelemetryChannel) => Promise<void>; onCancel: () => void; telemetryTypes: string[] }) {
  const [t, setT] = useState<TelemetryChannel>(initial ?? {
    channelName: "", readIntervalTicks: 1000, telemetryType: "", dataType: 0, unit: "", decimals: null,
  });
  const update = (patch: Partial<TelemetryChannel>) => setT(prev => ({ ...prev, ...patch }));
  return (
    <div className="grid grid-cols-2 gap-2 p-2 border border-zinc-700 rounded bg-zinc-950">
      <Field label="Channel name"><input className={inputClass} value={t.channelName} onChange={e => update({ channelName: e.target.value })} disabled={!isNew} /></Field>
      <Field label="Telemetry type">
        <input className={inputClass + " font-mono"} value={t.telemetryType} onChange={e => update({ telemetryType: e.target.value })} list="telemetry-type-options" placeholder="e.g. LteCar.Onboard.Telemetry.CpuTemperatureReader" />
      </Field>
      <Field label="Read interval (ms)"><input className={inputClass} type="number" value={t.readIntervalTicks} onChange={e => update({ readIntervalTicks: Number(e.target.value) })} /></Field>
      <Field label="Data type">
        <select className={inputClass} value={t.dataType} onChange={e => update({ dataType: Number(e.target.value) })}>
          {DATA_TYPES.map((d, i) => <option key={d} value={i}>{d}</option>)}
        </select>
      </Field>
      <Field label="Unit"><input className={inputClass} value={t.unit ?? ""} onChange={e => update({ unit: e.target.value })} /></Field>
      <Field label="Decimals"><input className={inputClass} type="number" value={t.decimals ?? ""} onChange={e => update({ decimals: e.target.value === "" ? null : Number(e.target.value) })} /></Field>
      <div className="col-span-2 flex gap-2 justify-end">
        <button className={btnGhost} onClick={onCancel}>Cancel</button>
        <button className={btnPrimary} onClick={() => onSave(t)}>Save</button>
      </div>
    </div>
  );
}

function VideoEditor({
  initial, isNew, onSave, onCancel,
}: { initial?: VideoStream; isNew: boolean; onSave: (v: VideoStream) => Promise<void>; onCancel: () => void }) {
  const [v, setV] = useState<VideoStream>(initial ?? {
    streamId: "", name: "", type: "camera", location: "", enabled: true, width: 1280, height: 720, framerate: 30, bitrateKbps: 1500,
  });
  const update = (patch: Partial<VideoStream>) => setV(prev => ({ ...prev, ...patch }));
  return (
    <div className="grid grid-cols-2 gap-2 p-2 border border-zinc-700 rounded bg-zinc-950">
      <Field label="Stream ID"><input className={inputClass} value={v.streamId} onChange={e => update({ streamId: e.target.value })} disabled={!isNew} /></Field>
      <Field label="Name"><input className={inputClass} value={v.name ?? ""} onChange={e => update({ name: e.target.value })} /></Field>
      <Field label="Type"><input className={inputClass} value={v.type ?? ""} onChange={e => update({ type: e.target.value })} /></Field>
      <Field label="Location"><input className={inputClass} value={v.location ?? ""} onChange={e => update({ location: e.target.value })} /></Field>
      <Field label="Enabled"><input type="checkbox" checked={v.enabled} onChange={e => update({ enabled: e.target.checked })} /></Field>
      <Field label="Width"><input className={inputClass} type="number" value={v.width ?? ""} onChange={e => update({ width: e.target.value === "" ? null : Number(e.target.value) })} /></Field>
      <Field label="Height"><input className={inputClass} type="number" value={v.height ?? ""} onChange={e => update({ height: e.target.value === "" ? null : Number(e.target.value) })} /></Field>
      <Field label="Framerate"><input className={inputClass} type="number" value={v.framerate ?? ""} onChange={e => update({ framerate: e.target.value === "" ? null : Number(e.target.value) })} /></Field>
      <Field label="Bitrate (kbps)"><input className={inputClass} type="number" value={v.bitrateKbps ?? ""} onChange={e => update({ bitrateKbps: e.target.value === "" ? null : Number(e.target.value) })} /></Field>
      <div className="col-span-2 flex gap-2 justify-end">
        <button className={btnGhost} onClick={onCancel}>Cancel</button>
        <button className={btnPrimary} onClick={() => onSave(v)}>Save</button>
      </div>
    </div>
  );
}

export default function ChannelsPage() {
  const { messages } = useI18n();
  const router = useRouter();
  const carId = router.query.carId as string;
  const carIdNum = carId ? parseInt(carId) : undefined;

  const [controls, setControls] = useState<ControlChannel[]>([]);
  const [telemetries, setTelemetries] = useState<TelemetryChannel[]>([]);
  const [videos, setVideos] = useState<VideoStream[]>([]);
  const [availableControlTypes, setAvailableControlTypes] = useState<string[]>([]);
  const [availableTelemetryTypes, setAvailableTelemetryTypes] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingControl, setEditingControl] = useState<{ key: string; channel: ControlChannel; isNew: boolean } | null>(null);
  const [editingTelemetry, setEditingTelemetry] = useState<{ key: string; channel: TelemetryChannel; isNew: boolean } | null>(null);
  const [editingVideo, setEditingVideo] = useState<{ key: string; stream: VideoStream; isNew: boolean } | null>(null);

  const reload = useCallback(async () => {
    if (!carIdNum) return;
    setLoading(true);
    try {
      const r = await fetch(`/api/cars/${carIdNum}/channels`);
      if (!r.ok) throw new Error(await r.text());
      const map = await r.json();
      setControls(Object.entries(map.controlChannels ?? {}).map(([k, v]: [string, any]) => ({
        channelName: k,
        displayName: v.displayName ?? null,
        isEnabled: v.isEnabled ?? true,
        requiresAxis: v.requiresAxis ?? false,
        maxResendInterval: v.maxResendInterval ?? null,
        controlType: v.controlType ?? "",
        pinManager: v.pinManager ?? "default",
        address: v.address ?? null,
        testDisabled: v.testDisabled ?? false,
        optionsJson: v.optionsJson ?? (v.options && Object.keys(v.options).length > 0 ? JSON.stringify(v.options) : ""),
        modifiedAt: v.modifiedAt ?? null,
      })));
      setTelemetries(Object.entries(map.telemetryChannels ?? {}).map(([k, v]: [string, any]) => ({
        channelName: k,
        readIntervalTicks: v.readIntervalTicks ?? 1000,
        telemetryType: v.telemetryType ?? "",
        dataType: v.dataType ?? 0,
        unit: v.unit ?? null,
        decimals: v.decimals ?? null,
        modifiedAt: v.modifiedAt ?? null,
      })));
      setVideos(Object.entries(map.videoStreams ?? {}).map(([k, v]: [string, any]) => ({
        streamId: k,
        name: v.name ?? null,
        type: v.type ?? null,
        location: v.location ?? null,
        enabled: v.enabled ?? false,
        width: v.width ?? null,
        height: v.height ?? null,
        framerate: v.framerate ?? null,
        bitrateKbps: v.bitrateKbps ?? (typeof v.bitrate === "number" ? v.bitrate : null),
        modifiedAt: v.modifiedAt ?? null,
      })));
    } catch (e) {
      console.error("Failed to load channels:", e);
    } finally {
      setLoading(false);
    }
  }, [carIdNum]);

  useEffect(() => { reload(); }, [reload]);

  // ponytail: fetch autocomplete data in parallel with channel reload. The
  // Onboard reports these via SignalR after OpenCarConnection; the Server
  // caches them per carId. Empty array when the Onboard is offline — the
  // <datalist> just renders no <option>s, falling back to free-text.
  useEffect(() => {
    if (!carIdNum) return;
    (async () => {
      const [c, t] = await Promise.all([
        fetch(`/api/cars/${carIdNum}/channels/available-control-types`).then(r => r.ok ? r.json() : []),
        fetch(`/api/cars/${carIdNum}/channels/available-telemetry-types`).then(r => r.ok ? r.json() : []),
      ]);
      if (Array.isArray(c)) setAvailableControlTypes(c);
      if (Array.isArray(t)) setAvailableTelemetryTypes(t);
    })();
  }, [carIdNum]);

  const saveControl = async (origKey: string, ch: ControlChannel, isNew: boolean) => {
    if (!carIdNum) return;
    let options: any = undefined;
    if (ch.optionsJson && ch.optionsJson.trim()) {
      try { options = JSON.parse(ch.optionsJson); }
      catch { alert("Options JSON invalid"); return; }
    }
    const body: any = {
      channelName: ch.channelName,
      displayName: ch.displayName,
      isEnabled: ch.isEnabled,
      requiresAxis: ch.requiresAxis,
      maxResendInterval: ch.maxResendInterval,
      controlType: ch.controlType,
      pinManager: ch.pinManager,
      address: ch.address,
      testDisabled: ch.testDisabled,
      options,
    };
    const r = await fetch(`/api/cars/${carIdNum}/channels/control/${encodeURIComponent(isNew ? ch.channelName : origKey)}`, {
      method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body),
    });
    if (!r.ok) { alert(await r.text()); return; }
    setEditingControl(null);
    reload();
  };

  const deleteControl = async (key: string) => {
    if (!carIdNum || !confirm(`Delete control channel "${key}"?`)) return;
    const r = await fetch(`/api/cars/${carIdNum}/channels/control/${encodeURIComponent(key)}`, { method: "DELETE" });
    if (!r.ok) { alert(await r.text()); return; }
    reload();
  };

  const saveTelemetry = async (origKey: string, t: TelemetryChannel, isNew: boolean) => {
    if (!carIdNum) return;
    const body = {
      channelName: t.channelName,
      readIntervalTicks: t.readIntervalTicks,
      telemetryType: t.telemetryType,
      dataType: t.dataType,
      unit: t.unit,
      decimals: t.decimals,
    };
    const r = await fetch(`/api/cars/${carIdNum}/channels/telemetry/${encodeURIComponent(isNew ? t.channelName : origKey)}`, {
      method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body),
    });
    if (!r.ok) { alert(await r.text()); return; }
    setEditingTelemetry(null);
    reload();
  };

  const deleteTelemetry = async (key: string) => {
    if (!carIdNum || !confirm(`Delete telemetry channel "${key}"?`)) return;
    const r = await fetch(`/api/cars/${carIdNum}/channels/telemetry/${encodeURIComponent(key)}`, { method: "DELETE" });
    if (!r.ok) { alert(await r.text()); return; }
    reload();
  };

  const saveVideo = async (origKey: string, v: VideoStream, isNew: boolean) => {
    if (!carIdNum) return;
    const body = {
      streamId: v.streamId, name: v.name, type: v.type, location: v.location, enabled: v.enabled,
      width: v.width, height: v.height, framerate: v.framerate, bitrateKbps: v.bitrateKbps,
    };
    const r = await fetch(`/api/cars/${carIdNum}/channels/video/${encodeURIComponent(isNew ? v.streamId : origKey)}`, {
      method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body),
    });
    if (!r.ok) { alert(await r.text()); return; }
    setEditingVideo(null);
    reload();
  };

  const deleteVideo = async (key: string) => {
    if (!carIdNum || !confirm(`Delete video stream "${key}"?`)) return;
    const r = await fetch(`/api/cars/${carIdNum}/channels/video/${encodeURIComponent(key)}`, { method: "DELETE" });
    if (!r.ok) { alert(await r.text()); return; }
    reload();
  };

  if (!carIdNum) return <div className="p-8 text-zinc-300">{messages.common.loading}</div>;
  if (loading) return <div className="p-8 text-zinc-300">{messages.common.loading}</div>;

  return (
    <ConfigGuard carId={carIdNum}>
      <div className="min-h-screen bg-zinc-950 text-zinc-100 p-4">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <Link href={`/car/${carIdNum}`} className="text-blue-400 hover:underline text-sm">&larr; Back</Link>
            <h1 className="text-lg">Channels — car {carIdNum}</h1>
          </div>
          <div className="flex gap-2">
            <LanguageSwitcher />
            <button className={btnGhost} onClick={reload}>Reload</button>
          </div>
        </div>

        <CollapsibleSection title={`Control channels (${controls.length})`} defaultCollapsed={false}>
          <button className={btnPrimary + " mb-2"} onClick={() => setEditingControl({ key: "__new__", channel: {} as ControlChannel, isNew: true })}>+ Add control channel</button>
          <div className="space-y-1">
            {controls.map(c => (
              <div key={c.channelName} className="border border-zinc-800 rounded p-2 bg-zinc-900">
                {editingControl?.key === c.channelName && !editingControl.isNew ? (
                  <ControlEditor initial={c} isNew={false} onSave={(nc) => saveControl(c.channelName, nc, false)} onCancel={() => setEditingControl(null)} controlTypes={availableControlTypes} />
                ) : (
                  <div className="flex items-center justify-between">
                    <div className="font-mono text-xs">
                      <span className="text-zinc-100">{c.channelName}</span>
                      <span className="text-zinc-500 ml-2">{c.controlType || "?"} @ {c.pinManager}#{c.address ?? "-"}</span>
                      {c.displayName && <span className="text-zinc-400 ml-2">({c.displayName})</span>}
                    </div>
                    <div className="flex gap-1">
                      <button className={btnGhost} onClick={() => setEditingControl({ key: c.channelName, channel: c, isNew: false })}>Edit</button>
                      <button className={btnDanger} onClick={() => deleteControl(c.channelName)}>Delete</button>
                    </div>
                  </div>
                )}
              </div>
            ))}
            {editingControl?.isNew && (
              <ControlEditor isNew={true} onSave={(nc) => saveControl("__new__", nc, true)} onCancel={() => setEditingControl(null)} controlTypes={availableControlTypes} />
            )}
          </div>
        </CollapsibleSection>

        <CollapsibleSection title={`Telemetry channels (${telemetries.length})`} defaultCollapsed={false}>
          <button className={btnPrimary + " mb-2"} onClick={() => setEditingTelemetry({ key: "__new__", channel: {} as TelemetryChannel, isNew: true })}>+ Add telemetry channel</button>
          <div className="space-y-1">
            {telemetries.map(t => (
              <div key={t.channelName} className="border border-zinc-800 rounded p-2 bg-zinc-900">
                {editingTelemetry?.key === t.channelName && !editingTelemetry.isNew ? (
                  <TelemetryEditor initial={t} isNew={false} onSave={(nt) => saveTelemetry(t.channelName, nt, false)} onCancel={() => setEditingTelemetry(null)} telemetryTypes={availableTelemetryTypes} />
                ) : (
                  <div className="flex items-center justify-between">
                    <div className="font-mono text-xs">
                      <span className="text-zinc-100">{t.channelName}</span>
                      <span className="text-zinc-500 ml-2">{t.telemetryType || "?"} / {DATA_TYPES[t.dataType] ?? "?"} {t.unit ? `(${t.unit})` : ""}</span>
                    </div>
                    <div className="flex gap-1">
                      <button className={btnGhost} onClick={() => setEditingTelemetry({ key: t.channelName, channel: t, isNew: false })}>Edit</button>
                      <button className={btnDanger} onClick={() => deleteTelemetry(t.channelName)}>Delete</button>
                    </div>
                  </div>
                )}
              </div>
            ))}
            {editingTelemetry?.isNew && (
              <TelemetryEditor isNew={true} onSave={(nt) => saveTelemetry("__new__", nt, true)} onCancel={() => setEditingTelemetry(null)} telemetryTypes={availableTelemetryTypes} />
            )}
          </div>
        </CollapsibleSection>

        <CollapsibleSection title={`Video streams (${videos.length})`} defaultCollapsed={false}>
          <button className={btnPrimary + " mb-2"} onClick={() => setEditingVideo({ key: "__new__", stream: {} as VideoStream, isNew: true })}>+ Add video stream</button>
          <div className="space-y-1">
            {videos.map(v => (
              <div key={v.streamId} className="border border-zinc-800 rounded p-2 bg-zinc-900">
                {editingVideo?.key === v.streamId && !editingVideo.isNew ? (
                  <VideoEditor initial={v} isNew={false} onSave={(nv) => saveVideo(v.streamId, nv, false)} onCancel={() => setEditingVideo(null)} />
                ) : (
                  <div className="flex items-center justify-between">
                    <div className="font-mono text-xs">
                      <span className="text-zinc-100">{v.streamId}</span>
                      <span className="text-zinc-500 ml-2">{v.name || "?"} {v.type ? `(${v.type})` : ""} {v.location ? `@ ${v.location}` : ""}</span>
                    </div>
                    <div className="flex gap-1">
                      <button className={btnGhost} onClick={() => setEditingVideo({ key: v.streamId, stream: v, isNew: false })}>Edit</button>
                      <button className={btnDanger} onClick={() => deleteVideo(v.streamId)}>Delete</button>
                    </div>
                  </div>
                )}
              </div>
            ))}
            {editingVideo?.isNew && (
              <VideoEditor isNew={true} onSave={(nv) => saveVideo("__new__", nv, true)} onCancel={() => setEditingVideo(null)} />
            )}
          </div>
        </CollapsibleSection>

        <datalist id="control-type-options">
          {availableControlTypes.map(t => <option key={t} value={t} />)}
        </datalist>
        <datalist id="telemetry-type-options">
          {availableTelemetryTypes.map(t => <option key={t} value={t} />)}
        </datalist>
      </div>
    </ConfigGuard>
  );
}