'use client';

import { JSX, useEffect, useMemo, useRef, useState } from 'react';
import VideoStream from './video-stream';
import type { VideoStreamInfo } from '@/types/video-stream';
import { useCarUiStore } from './car-ui-store';
import { useI18n } from '@/i18n/provider';

const STREAM_REFRESH_EVENT = 'videoStreams:refresh';

type CarVideoPanelProps = {
  carId?: number;
};

type SectionId = 'selection' | 'status' | 'video';

const DEFAULT_SECTIONS: SectionId[] = ['selection', 'status', 'video'];
const DEFAULT_COLLAPSED: Record<SectionId, boolean> = {
  selection: false,
  status: false,
  video: false,
};

function getStoredStreamId(carId: number): number | undefined {
  const rawValue = window.localStorage.getItem(`lastSelectedStreamId:${carId}`);
  if (!rawValue) {
    return undefined;
  }

  const parsed = Number(rawValue);
  return Number.isInteger(parsed) ? parsed : undefined;
}

function loadSectionLayout(carId: number): {
  order: SectionId[];
  collapsed: Record<SectionId, boolean>;
} {
  let order = DEFAULT_SECTIONS;
  try {
    const raw = window.localStorage.getItem(`videoPanel:order:${carId}`);
    if (raw) {
      const parsed = JSON.parse(raw) as SectionId[];
      if (Array.isArray(parsed)) {
        const known = parsed.filter((id): id is SectionId => DEFAULT_SECTIONS.includes(id));
        const missing = DEFAULT_SECTIONS.filter(id => !known.includes(id));
        if (known.length + missing.length === DEFAULT_SECTIONS.length) {
          order = [...known, ...missing];
        }
      }
    }
  } catch {
    // ponytail: corrupt localStorage entry → fall back to defaults
  }

  let collapsed: Record<SectionId, boolean> = DEFAULT_COLLAPSED;
  try {
    const raw = window.localStorage.getItem(`videoPanel:collapsed:${carId}`);
    if (raw) {
      const parsed = JSON.parse(raw) as Partial<Record<SectionId, boolean>>;
      collapsed = { ...DEFAULT_COLLAPSED, ...parsed };
    }
  } catch {
    // ponytail: corrupt localStorage entry → fall back to defaults
  }

  return { order, collapsed };
}

function sortStreams(streams: VideoStreamInfo[]): VideoStreamInfo[] {
  return [...streams].sort((left, right) => {
    if (left.priority !== right.priority) {
      return left.priority - right.priority;
    }

    return left.name.localeCompare(right.name, 'de');
  });
}

export default function CarVideoPanel({ carId }: CarVideoPanelProps): JSX.Element {
  const { messages } = useI18n();
  const [videoConnection, setVideoConnection] = useState<any>(undefined);
  const [streams, setStreams] = useState<VideoStreamInfo[]>([]);
  const [selectedStreamId, setSelectedStreamId] = useState<number | undefined>(undefined);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [documentVisible, setDocumentVisible] = useState(true);
  const [reconnectVersion, setReconnectVersion] = useState(0);
  const connectCarUi = useCarUiStore(state => state.connect);

  const [sectionOrder, setSectionOrder] = useState<SectionId[]>(DEFAULT_SECTIONS);
  const [sectionCollapsed, setSectionCollapsed] = useState<Record<SectionId, boolean>>(DEFAULT_COLLAPSED);
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);
  const dragRef = useRef<number | null>(null);

  useEffect(() => {
    void connectCarUi();
  }, [connectCarUi]);

  useEffect(() => {
    if (typeof document === 'undefined') {
      return;
    }

    const updateVisibility = () => setDocumentVisible(!document.hidden);
    updateVisibility();
    document.addEventListener('visibilitychange', updateVisibility);

    return () => document.removeEventListener('visibilitychange', updateVisibility);
  }, []);

  useEffect(() => {
    if (!carId) {
      setSectionOrder(DEFAULT_SECTIONS);
      setSectionCollapsed(DEFAULT_COLLAPSED);
      return;
    }
    const { order, collapsed } = loadSectionLayout(carId);
    setSectionOrder(order);
    setSectionCollapsed(collapsed);
  }, [carId]);

  useEffect(() => {
    if (!carId) {
      return;
    }
    window.localStorage.setItem(`videoPanel:order:${carId}`, JSON.stringify(sectionOrder));
  }, [carId, sectionOrder]);

  useEffect(() => {
    if (!carId) {
      return;
    }
    window.localStorage.setItem(`videoPanel:collapsed:${carId}`, JSON.stringify(sectionCollapsed));
  }, [carId, sectionCollapsed]);

  useEffect(() => {
    let mounted = true;
    let conn: any;

    (async () => {
      try {
        const signalR = await import('@microsoft/signalr');
        conn = new signalR.HubConnectionBuilder()
          .withUrl('/hubs/connection')
          .withAutomaticReconnect()
          .build();

        conn.onreconnected(() => {
          setReconnectVersion(currentVersion => currentVersion + 1);
          window.dispatchEvent(new Event(STREAM_REFRESH_EVENT));
        });

        await conn.start();
        if (!mounted) {
          try {
            await conn.stop();
          } catch {}
          return;
        }

        setVideoConnection(conn);
      } catch (connectionError) {
        console.debug('Failed to start video hub connection:', connectionError);
        if (mounted) {
          setError(messages.carVideoPanel.hubConnectionFailed);
        }
      }
    })();

    return () => {
      mounted = false;
      if (conn) {
        try {
          conn.stop();
        } catch {}
      }
      setVideoConnection(undefined);
    };
  }, [messages]);

  useEffect(() => {
    if (!carId || !videoConnection) {
      setStreams([]);
      setSelectedStreamId(undefined);
      setLoading(false);
      return;
    }

    let cancelled = false;

    const loadStreams = async () => {
      setLoading(true);
      setError(null);
      try {
        const result = await videoConnection.invoke('GetVideoStreamsForCar', carId) as VideoStreamInfo[];
        if (cancelled) {
          return;
        }

        setStreams(sortStreams(result ?? []));
      } catch (loadError) {
        if (cancelled) {
          return;
        }

        console.error('Failed to load video streams:', loadError);
        setError(messages.carVideoPanel.loadStreamsFailed);
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    const handleRefresh = () => {
      void loadStreams();
    };

    void loadStreams();
    window.addEventListener(STREAM_REFRESH_EVENT, handleRefresh);

    return () => {
      cancelled = true;
      window.removeEventListener(STREAM_REFRESH_EVENT, handleRefresh);
    };
  }, [carId, reconnectVersion, videoConnection, messages]);

  useEffect(() => {
    if (!carId) {
      setSelectedStreamId(undefined);
      return;
    }

    if (streams.length === 0) {
      setSelectedStreamId(undefined);
      return;
    }

    setSelectedStreamId(currentValue => {
      const hasCurrentSelection = currentValue !== undefined && streams.some(stream => stream.id === currentValue);
      if (hasCurrentSelection) {
        return currentValue;
      }

      const storedStreamId = getStoredStreamId(carId);
      if (storedStreamId !== undefined && streams.some(stream => stream.id === storedStreamId)) {
        return storedStreamId;
      }

      return streams.find(stream => stream.enabled)?.id ?? streams[0]?.id;
    });
  }, [carId, streams]);

  useEffect(() => {
    if (!carId || selectedStreamId === undefined) {
      return;
    }

    window.localStorage.setItem(`lastSelectedStreamId:${carId}`, String(selectedStreamId));
  }, [carId, selectedStreamId]);

  const selectedStream = useMemo(
    () => streams.find(stream => stream.id === selectedStreamId),
    [selectedStreamId, streams],
  );

  useEffect(() => {
    if (!videoConnection || !selectedStream || !selectedStream.enabled || !documentVisible) {
      return;
    }

    let disposed = false;
    void videoConnection.invoke('ActivateStream', selectedStream.id).catch((activationError: unknown) => {
      if (!disposed) {
        console.error('Failed to activate stream:', activationError);
      }
    });

    return () => {
      disposed = true;
      void videoConnection.invoke('DeactivateStream', selectedStream.id).catch((deactivationError: unknown) => {
        console.error('Failed to deactivate stream:', deactivationError);
      });
    };
  }, [documentVisible, reconnectVersion, selectedStream, videoConnection]);

  const toggleCollapsed = (id: SectionId) => {
    setSectionCollapsed(prev => ({ ...prev, [id]: !prev[id] }));
  };

  const onDragStart = (e: React.DragEvent<HTMLButtonElement>, index: number) => {
    dragRef.current = index;
    setDragIndex(index);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', String(index));
  };

  const onDragOver = (e: React.DragEvent<HTMLElement>, index: number) => {
    if (dragIndex === null) {
      return;
    }
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    if (index !== dragOverIndex) {
      setDragOverIndex(index);
    }
  };

  const onDragLeave = (index: number) => {
    if (dragOverIndex === index) {
      setDragOverIndex(null);
    }
  };

  const onDrop = (e: React.DragEvent<HTMLElement>, dropIndex: number) => {
    e.preventDefault();
    const sourceIndex = dragRef.current;
    dragRef.current = null;
    setDragIndex(null);
    setDragOverIndex(null);
    if (sourceIndex === null || sourceIndex === dropIndex) {
      return;
    }
    setSectionOrder(prev => {
      const next = [...prev];
      const [moved] = next.splice(sourceIndex, 1);
      next.splice(dropIndex, 0, moved);
      return next;
    });
  };

  const onDragEnd = () => {
    dragRef.current = null;
    setDragIndex(null);
    setDragOverIndex(null);
  };

  if (!carId) {
    return (
      <div className="flex h-full w-full items-center justify-center rounded-2xl border border-dashed border-zinc-700 bg-zinc-900 p-10 text-center text-sm text-zinc-300">
        {messages.carVideoPanel.selectVehicleFirst}
      </div>
    );
  }

  if (loading && streams.length === 0) {
    return (
      <div className="flex h-full w-full items-center justify-center rounded-2xl border border-zinc-700 bg-zinc-900 text-sm text-zinc-300">
        {messages.carVideoPanel.loadingStreams}
      </div>
    );
  }

  if (error && streams.length === 0) {
    return (
      <div className="flex h-full w-full items-center justify-center rounded-2xl border border-red-900 bg-red-950/70 p-6 text-center text-sm text-red-200">
        {error}
      </div>
    );
  }

  if (streams.length === 0) {
    return (
      <div className="flex h-full w-full items-center justify-center rounded-2xl border border-zinc-700 bg-zinc-900 p-6 text-center text-sm text-zinc-300">
        {messages.carVideoPanel.noStreamsConfigured}
      </div>
    );
  }

  const sectionLabels: Record<SectionId, string> = {
    selection: messages.carVideoPanel.sectionSelection,
    status: messages.carVideoPanel.sectionStatus,
    video: messages.carVideoPanel.sectionVideo,
  };

  const renderSelection = () => (
    <div className="flex flex-wrap gap-2">
      {streams.map(stream => {
        const isSelected = stream.id === selectedStreamId;
        const buttonClasses = isSelected
          ? 'border-zinc-900 bg-zinc-900 text-white'
          : 'border-zinc-300 bg-white text-zinc-700 hover:border-zinc-500';

        return (
          <button
            key={stream.id}
            type="button"
            className={`rounded-full border px-3 py-2 text-sm transition-colors ${buttonClasses}`}
            onClick={() => setSelectedStreamId(stream.id)}
          >
            <span className="font-medium">{stream.name}</span>
            <span className="ml-2 text-xs opacity-80">
              {stream.location ? `${stream.location} · ` : ''}
              {stream.enabled ? messages.carVideoPanel.streamEnabled : messages.carVideoPanel.streamDisabled}
              {stream.viewerCount > 0 ? ` · ${messages.carVideoPanel.viewerCount(stream.viewerCount)}` : ''}
            </span>
          </button>
        );
      })}
    </div>
  );

  const renderStatus = () => (
    <div className="flex flex-col gap-2 text-sm text-zinc-300">
      {!selectedStream && (
        <div className="rounded-xl border border-zinc-700 bg-zinc-900/70 p-3">
          {messages.carVideoPanel.noStreamSelected}
        </div>
      )}
      {selectedStream && !selectedStream.enabled && (
        <div className="rounded-xl border border-amber-900 bg-amber-950/50 p-3 text-amber-200">
          {messages.carVideoPanel.streamDisabledHint}
        </div>
      )}
      {selectedStream && selectedStream.enabled && !documentVisible && (
        <div className="rounded-xl border border-zinc-700 bg-zinc-900/70 p-3">
          {messages.carVideoPanel.tabInactiveHint}
        </div>
      )}
    </div>
  );

  const renderVideo = () => {
    if (!selectedStream) return null;
    if (!selectedStream.enabled) return null;
    if (!documentVisible) return null;
    return (
      <div className="flex h-[60vh] min-h-[20rem] overflow-hidden rounded-2xl border border-zinc-200 bg-black shadow-sm">
        <VideoStream key={selectedStream.id} streamId={selectedStream.id} streamName={selectedStream.name} />
      </div>
    );
  };

  const renderSectionBody = (id: SectionId): JSX.Element => {
    switch (id) {
      case 'selection':
        return renderSelection();
      case 'status':
        return renderStatus();
      case 'video':
        return renderVideo() ?? <div className="rounded-xl border border-zinc-700 bg-zinc-900/70 p-3 text-zinc-400">—</div>;
    }
  };

  return (
    <div className="flex h-full w-full min-h-0 flex-col gap-3 overflow-y-auto pr-1">
      {sectionOrder.map((id, index) => {
        const collapsed = sectionCollapsed[id];
        const isDragging = dragIndex === index;
        const isDropTarget = dragOverIndex === index && dragIndex !== null && dragIndex !== index;
        return (
          <section
            key={id}
            className={`flex shrink-0 flex-col rounded-2xl border border-zinc-200/60 bg-white/85 shadow-sm transition-shadow ${
              isDragging ? 'opacity-50' : ''
            } ${isDropTarget ? 'ring-2 ring-sky-400/60' : ''}`}
            onDragOver={(e) => onDragOver(e, index)}
            onDragLeave={() => onDragLeave(index)}
            onDrop={(e) => onDrop(e, index)}
          >
            <header className="flex items-center justify-between gap-2 rounded-t-2xl bg-zinc-100/80 px-3 py-2 text-xs font-semibold uppercase tracking-wider text-zinc-700">
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  draggable
                  onDragStart={(e) => onDragStart(e, index)}
                  onDragEnd={onDragEnd}
                  aria-label={messages.carVideoPanel.dragHandle}
                  className="cursor-grab select-none rounded p-1 text-zinc-500 hover:bg-zinc-200 active:cursor-grabbing"
                  title={messages.carVideoPanel.dragHandle}
                >
                  ⋮⋮
                </button>
                <span>{sectionLabels[id]}</span>
              </div>
              <button
                type="button"
                onClick={() => toggleCollapsed(id)}
                aria-label={collapsed ? messages.common.expandSection(sectionLabels[id]) : messages.common.collapseSection(sectionLabels[id])}
                className="rounded p-1 text-zinc-500 hover:bg-zinc-200"
              >
                {collapsed ? '▼' : '▲'}
              </button>
            </header>
            {!collapsed && (
              <div className="px-3 py-3">
                {renderSectionBody(id)}
              </div>
            )}
          </section>
        );
      })}
    </div>
  );
}
