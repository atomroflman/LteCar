import React, { JSX, useEffect, useState } from "react";
import { useControlFlowStore } from "./control-flow-store";
import CollapsibleSection from "./collapsible-section";

type StreamInfo = {
  id: number;
  name: string;
  streamId: string;
  protocol?: string;
  port?: number;
  encoding?: string;
  width?: number | null;
  height?: number | null;
  bitrateKbps?: number | null;
  framerate?: number | null;
  brightness?: number | null; // 0..1 float on server
};

type VideoSettings = {
  height?: number | null;
  width?: number | null;
  framerate?: number | null;
  bitrate?: number | null; // bytes per second
  brightness?: number | null; // 0..1
};

export default function VideoSettingsControl(props: { carId?: number } = {}): JSX.Element {
  const carIdFromStore = useControlFlowStore(state => state.carId);
  const carId = props.carId ?? carIdFromStore;

  const [videoConnection, setVideoConnection] = useState<any>(undefined);

  const [streams, setStreams] = useState<StreamInfo[]>([]);
  const [settingsMap, setSettingsMap] = useState<Record<number, VideoSettings & { resolutionMode?: 'preset'|'custom'; preset?: string }>>({});
  const [busyMap, setBusyMap] = useState<Record<number, boolean>>({});
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setError(null);
    if (!carId) {
      setStreams([]);
      setSettingsMap({});
      return;
    }

    // Prefer the dedicated CarVideoHub connection when available
    const hub = videoConnection;
    // Try SignalR hub first
    if (hub) {        
      hub.invoke('GetVideoStreamsForCar', carId)
        .then((data: Record<string, any>) => {
          const list: StreamInfo[] = Object.entries(data).map(([k, v]) => ({
            id: Number(k),
            name: v?.StreamId ?? `Stream ${k}`,
            streamId: v?.StreamId,
            protocol: v?.Protocol,
            port: v?.Port,
            encoding: v?.Encoding,
            width: v?.Width ?? null,
            height: v?.Height ?? null,
            bitrateKbps: v?.BitrateKbps ?? null,
            framerate: v?.Framerate ?? null,
            brightness: v?.Brightness ?? null,
          }));

          setStreams(list);
          const map: Record<number, any> = {};
          list.forEach(s => {
            map[s.id] = {
              width: s.width ?? null,
              height: s.height ?? null,
              framerate: s.framerate ?? null,
              bitrate: s.bitrateKbps ? s.bitrateKbps * 1024 : null,
              brightness: s.brightness ?? null,
              resolutionMode: 'preset'
            };
          });
          setSettingsMap(map);
        });
      return;
    }
  }, [videoConnection, carId]);

  // Manage lifecycle of the CarVideoHub connection (component-local)
  useEffect(() => {
    let mounted = true;
    let conn: any;
    (async () => {
      try {
        const signalR = await import("@microsoft/signalr");
        conn = new signalR.HubConnectionBuilder()
          .withUrl('/hubs/video')
          .withAutomaticReconnect()
          .build();

        await conn.start();
        if (!mounted) {
          try { await conn.stop(); } catch {}
          return;
        }
        setVideoConnection(conn);
      } catch (err) {
        // If we fail to create a video hub connection, leave videoConnection undefined
        console.debug('Failed to start video hub connection:', err);
      }
    })();

    return () => {
      mounted = false;
      if (conn) {
        try { conn.stop(); } catch {}
      }
      setVideoConnection(undefined);
    };
  }, []);

  const resolutionPresets = [
    { key: '1920x1080', w: 1920, h: 1080 },
    { key: '1280x720', w: 1280, h: 720 },
    { key: '800x600', w: 800, h: 600 },
    { key: '640x480', w: 640, h: 480 },
    { key: '3840x2160', w: 3840, h: 2160 },
    { key: 'custom', w: null, h: null }
  ];

  function updateFieldFor(streamId: number, key: keyof VideoSettings, value: any) {
    setSettingsMap(m => ({ ...m, [streamId]: { ...(m[streamId] || {}), [key]: value } }));
  }

  async function handleSave(streamId: number) {
    setError(null);
    const cfg = settingsMap[streamId];
    if (!cfg) return setError('No settings for stream');
    setBusyMap(b => ({ ...b, [streamId]: true }));
    const payload = {
      height: cfg.height ?? null,
      width: cfg.width ?? null,
      framerate: cfg.framerate ?? null,
      bitrate: cfg.bitrate ?? null,
      brightness: cfg.brightness ?? null,
    };

    try {
      const hub = videoConnection;
      if (hub) {
        await hub.invoke('ChangeVideoStreamSettings', streamId, payload);
      } else {
        const res = await fetch(`/api/video/streams/${streamId}/settings`, { method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
        if (!res.ok) window.dispatchEvent(new CustomEvent('videoSettings:change', { detail: { streamId, settings: payload } }));
      }
    } catch (e) {
      window.dispatchEvent(new CustomEvent('videoSettings:change', { detail: { streamId, settings: payload } }));
    } finally {
      setBusyMap(b => ({ ...b, [streamId]: false }));
    }
  }

  async function handleStart(streamId: number) {
    setBusyMap(b => ({ ...b, [streamId]: true }));
    try {
      const hub = videoConnection;
      if (hub) await hub.invoke('StartVideoStream', streamId);
      else {
        const res = await fetch(`/api/video/streams/${streamId}/start`, { method: 'POST' });
        if (!res.ok) window.dispatchEvent(new CustomEvent('videoSettings:start', { detail: { streamId } }));
      }
    } catch {
      window.dispatchEvent(new CustomEvent('videoSettings:start', { detail: { streamId } }));
    } finally {
      setBusyMap(b => ({ ...b, [streamId]: false }));
    }
  }

  async function handleStop(streamId: number) {
    setBusyMap(b => ({ ...b, [streamId]: true }));
    try {
      const hub = videoConnection;
      if (hub) await hub.invoke('StopVideoStream', streamId);
      else {
        const res = await fetch(`/api/video/streams/${streamId}/stop`, { method: 'POST' });
        if (!res.ok) window.dispatchEvent(new CustomEvent('videoSettings:stop', { detail: { streamId } }));
      }
    } catch {
      window.dispatchEvent(new CustomEvent('videoSettings:stop', { detail: { streamId } }));
    } finally {
      setBusyMap(b => ({ ...b, [streamId]: false }));
    }
  }

  return (
    <CollapsibleSection title="Video-Einstellungen" defaultCollapsed={true} className="px-2">
      <div className="space-y-2 text-xs leading-tight">
        {streams.length === 0 && <div className="text-zinc-400">Keine Streams gefunden.</div>}

        {streams.map(s => {
          const cfg = settingsMap[s.id] || {};
          const busy = !!busyMap[s.id];
          const presetValue = cfg.resolutionMode === 'custom' ? 'custom' : ((cfg.width ?? s.width) && (cfg.height ?? s.height) ? `${cfg.width ?? s.width}x${cfg.height ?? s.height}` : '');

          return (
            <div key={s.id} className="mb-2 p-2 bg-zinc-800 border border-zinc-700 rounded">
              <div className="flex items-center justify-between mb-2">
                <div className="font-medium text-zinc-100">{s.name || s.streamId} <span className="text-[11px] text-zinc-400">(#{s.id})</span></div>
                <div className="text-[11px] text-zinc-400">{s.protocol || ''}{s.port ? `:${s.port}` : ''}</div>
              </div>

              <div className="grid grid-cols-2 gap-2 mb-2">
                <div>
                  <label className="block text-xs text-zinc-300">Auflösung</label>
                  <select className="w-full text-xs p-1 rounded bg-zinc-900 border border-zinc-700 text-zinc-100" value={presetValue} onChange={e => {
                    const val = e.target.value;
                    if (val === 'custom') {
                      setSettingsMap(m => ({ ...m, [s.id]: { ...(m[s.id]||{}), resolutionMode: 'custom', width: m[s.id]?.width ?? s.width ?? null, height: m[s.id]?.height ?? s.height ?? null } }));
                    } else {
                      const [w,h] = val.split('x').map(Number);
                      setSettingsMap(m => ({ ...m, [s.id]: { ...(m[s.id]||{}), resolutionMode: 'preset', width: w, height: h } }));
                    }
                  }}>
                    {resolutionPresets.map(p => (
                      <option key={p.key} value={p.key === 'custom' ? 'custom' : `${p.w}x${p.h}`}>{p.key === 'custom' ? 'Custom…' : `${p.w}×${p.h}`}</option>
                    ))}
                  </select>

                  {cfg.resolutionMode === 'custom' && (
                    <div className="mt-1 flex gap-1">
                      <input type="number" className="w-1/2 text-xs p-1 rounded bg-zinc-900 border border-zinc-700 text-zinc-100" value={cfg.width ?? ''} onChange={e => updateFieldFor(s.id, 'width', e.target.value ? Number(e.target.value) : null)} placeholder="Width" />
                      <input type="number" className="w-1/2 text-xs p-1 rounded bg-zinc-900 border border-zinc-700 text-zinc-100" value={cfg.height ?? ''} onChange={e => updateFieldFor(s.id, 'height', e.target.value ? Number(e.target.value) : null)} placeholder="Height" />
                    </div>
                  )}
                </div>

                <div>
                  <label className="block text-xs text-zinc-300">Framerate</label>
                  <input type="number" className="w-full text-xs p-1 rounded bg-zinc-900 border border-zinc-700 text-zinc-100" value={cfg.framerate ?? s.framerate ?? ''} onChange={e => updateFieldFor(s.id, 'framerate', e.target.value ? Number(e.target.value) : null)} />
                </div>

                <div>
                  <label className="block text-xs text-zinc-300">Bitrate (kbps)</label>
                  <input type="number" className="w-full text-xs p-1 rounded bg-zinc-900 border border-zinc-700 text-zinc-100" value={cfg.bitrate ? Math.round((cfg.bitrate ?? 0) / 1024) : (s.bitrateKbps ?? '')} onChange={e => updateFieldFor(s.id, 'bitrate', e.target.value ? Number(e.target.value) * 1024 : null)} />
                </div>

                <div>
                  <label className="block text-xs text-zinc-300">Helligkeit</label>
                  <input type="range" min="0" max="100" className="w-full" value={Math.round(((cfg.brightness ?? s.brightness ?? 0.5) as number) * 100)} onChange={e => updateFieldFor(s.id, 'brightness', Number(e.target.value) / 100)} />
                </div>
              </div>

              <div className="flex gap-2">
                <button className="px-2 py-1 text-xs rounded bg-zinc-700 hover:bg-zinc-600 text-zinc-100" onClick={() => handleSave(s.id)} disabled={busyMap[s.id]}>{busyMap[s.id] ? '...' : 'Speichern'}</button>
                <button className="px-2 py-1 text-xs rounded bg-green-700 hover:bg-green-600 text-zinc-100" onClick={() => handleStart(s.id)} disabled={busyMap[s.id]}>Start</button>
                <button className="px-2 py-1 text-xs rounded bg-zinc-700 hover:bg-zinc-600 text-zinc-100" onClick={() => handleStop(s.id)} disabled={busyMap[s.id]}>Stop</button>
              </div>
            </div>
          );
        })}

        {error && <div className="text-red-600 text-sm">{error}</div>}
      </div>
    </CollapsibleSection>
  );
}
