'use client';

import { useEffect, useRef, useState } from 'react';
import type { Janus, JanusPluginHandle, JanusStatic, JanusStreamingMessage } from '@/types/janus';

interface VideoStreamProps {
  carId?: number;
}

const JANUS_SERVERS = ['/janus', '/janus-ws'];
const JANUS_SCRIPT_ID = 'janus-gateway-sdk-script';

function parseBitrate(raw: unknown): number | null {
  if (raw === null || raw === undefined) return null;
  if (typeof raw === 'number') {
    return Number.isFinite(raw) ? raw : null;
  }
  if (typeof raw !== 'string') return null;

  const trimmed = raw.trim();
  if (!trimmed) return null;
  const lower = trimmed.toLowerCase();
  const numeric = parseFloat(trimmed.replace(/[^0-9.]/g, ''));
  if (!Number.isFinite(numeric)) return null;

  if (lower.includes('mbit')) {
    return numeric * 1_000_000;
  }
  if (lower.includes('kbit') || lower.includes('kbps')) {
    return numeric * 1_000;
  }
  if (lower.includes('bit')) {
    return numeric;
  }

  // Assume plain number is bits per second
  return numeric;
}

function formatBitrate(bits: number | null): string {
  if (bits === null || !Number.isFinite(bits)) {
    return '--';
  }
  if (bits >= 1_000_000) {
    return `${(bits / 1_000_000).toFixed(2)} Mbit/s`;
  }
  return `${(bits / 1_000).toFixed(0)} kbit/s`;
}

function loadJanusScript(): Promise<JanusStatic> {
  return new Promise((resolve, reject) => {
    if (typeof window === 'undefined') {
      reject(new Error('Browserumgebung erforderlich'));
      return;
    }

    if (window.Janus) {
      resolve(window.Janus);
      return;
    }

    const existing = document.getElementById(JANUS_SCRIPT_ID) as HTMLScriptElement | null;

    const cleanup = (script: HTMLScriptElement) => {
      script.removeEventListener('load', onLoad);
      script.removeEventListener('error', onError);
    };

    const onLoad = () => {
      const script = (document.getElementById(JANUS_SCRIPT_ID) as HTMLScriptElement | null);
      if (script) {
        cleanup(script);
      }
      if (window.Janus) {
        resolve(window.Janus);
      } else {
        reject(new Error('Janus global nicht verfügbar'));
      }
    };

    const onError = () => {
      const script = (document.getElementById(JANUS_SCRIPT_ID) as HTMLScriptElement | null);
      if (script) {
        cleanup(script);
      }
      reject(new Error('Janus Skript konnte nicht geladen werden'));
    };

    const script = existing ?? document.createElement('script');
    script.id = JANUS_SCRIPT_ID;
    script.async = true;
    script.src = '/janus.js';
    script.addEventListener('load', onLoad, { once: true });
    script.addEventListener('error', onError, { once: true });
    if (!existing) {
      document.body.appendChild(script);
    }
  });
}

export default function VideoStream({ carId }: VideoStreamProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const janusRef = useRef<Janus | null>(null);
  const pluginHandleRef = useRef<JanusPluginHandle | null>(null);
  const [pluginHandle, setPluginHandle] = useState<JanusPluginHandle | null>(null);
  const [streamStatus, setStreamStatus] = useState('Initializing...');
  const [error, setError] = useState<string | null>(null);
  const [bitrate, setBitrate] = useState<number | null>(null);
  const [fps, setFps] = useState<number | null>(null);
  const [overlayVisible, setOverlayVisible] = useState(true);

  useEffect(() => {
    let mounted = true;

    const resetState = () => {
      setStreamStatus('Initializing...');
      setError(null);
      setBitrate(null);
      setFps(null);
      setPluginHandle(null);
      pluginHandleRef.current = null;
      if (videoRef.current) {
        videoRef.current.srcObject = null;
      }
    };

    resetState();

    const initialise = async () => {
      try {
        const JanusCtor = await loadJanusScript();
        if (!mounted) return;

        if (!JanusCtor.isWebrtcSupported()) {
          setError('WebRTC is not supported by this browser');
          setStreamStatus('Error');
          return;
        }

        setStreamStatus('Connecting to Janus...');

        JanusCtor.init({
          debug: 'all',
          dependencies: JanusCtor.useDefaultDependencies(),
          callback: () => {
            if (!mounted) return;

            const janus = new JanusCtor({
              server: JANUS_SERVERS,
              success: () => {
                if (!mounted) return;

                setStreamStatus('Connected, loading plugin...');
                janusRef.current = janus;

                const handleRemoteTrack = (track: MediaStreamTrack, mid: string, on: boolean) => {
                  if (!mounted) return;
                  if (on && track.kind === 'video') {
                    const stream = new MediaStream([track]);
                    if (videoRef.current) {
                      videoRef.current.srcObject = stream;
                      setStreamStatus('Video stream active');
                      setError(null);
                    }
                  }
                };

                const handleRemoteStream = (stream: MediaStream) => {
                  if (!mounted) return;
                  if (videoRef.current) {
                    JanusCtor.attachMediaStream(videoRef.current, stream);
                    setStreamStatus('Video stream active');
                    setError(null);
                  }
                };

                const handleMessage = (msg: JanusStreamingMessage, jsep?: RTCSessionDescriptionInit) => {
                  if (!mounted) return;
                  if (jsep && pluginHandleRef.current) {
                    setStreamStatus('Negotiating WebRTC...');
                    pluginHandleRef.current.createAnswer({
                      jsep,
                      media: { audioSend: false, videoSend: false },
                      success: (answer) => {
                        if (!mounted) return;
                        pluginHandleRef.current?.send({
                          message: { request: 'start' },
                          jsep: answer,
                        });
                      },
                      error: (err) => {
                        if (!mounted) return;
                        console.error('createAnswer error:', err);
                        setError(`WebRTC error: ${err}`);
                        setStreamStatus('Error');
                      },
                    });
                  }
                };

                janus.attach({
                  plugin: 'janus.plugin.streaming',
                  success: (handle) => {
                    if (!mounted) return;

                    pluginHandleRef.current = handle;
                    setPluginHandle(handle);
                    setStreamStatus('Plugin ready, discovering streams...');

                    handle.onremotetrack = handleRemoteTrack;
                    handle.onremotestream = handleRemoteStream;

                    handle.send({
                      message: { request: 'list' },
                      success: (msg: JanusStreamingMessage) => {
                        if (!mounted) return;

                        const streams = msg.list ?? [];
                        if (msg.streaming === 'list' && streams.length > 0) {
                          let streamId: number;

                          if (typeof carId === 'number') {
                            const match = streams.find((s) => s.id === carId);
                            streamId = match ? match.id : streams[0].id;
                          } else {
                            streamId = streams[0].id;
                          }

                          setStreamStatus(`Starting stream ${streamId}...`);
                          handle.send({ message: { request: 'watch', id: streamId } });
                        } else {
                          setError('No video streams available');
                          setStreamStatus('Error');
                        }
                      },
                      error: (err: string) => {
                        if (!mounted) return;
                        console.error('Stream list error:', err);
                        setError(`Stream list error: ${err}`);
                        setStreamStatus('Error');
                      },
                    });
                  },
                  onmessage: handleMessage,
                  onremotetrack: handleRemoteTrack,
                  error: (err: string) => {
                    if (!mounted) return;
                    console.error('Plugin attach error:', err);
                    setError(`Plugin error: ${err}`);
                    setStreamStatus('Error');
                  },
                });
              },
              error: (err: string) => {
                if (!mounted) return;
                console.error('Janus init error:', err);
                setError(`Connection error: ${err}`);
                setStreamStatus('Error');
              },
              destroyed: () => {
                if (!mounted) return;
                setStreamStatus('Connection closed');
              },
            });
          },
        });
      } catch (err) {
        if (!mounted) return;
        console.error('Janus setup error:', err);
        const message = err instanceof Error ? err.message : String(err);
        setError(message);
        setStreamStatus('Error');
      }
    };

    initialise();

    return () => {
      mounted = false;
      if (pluginHandleRef.current) {
        try {
          pluginHandleRef.current.detach();
        } catch (detachError) {
          console.warn('Janus detach error:', detachError);
        }
        pluginHandleRef.current = null;
      }
      if (janusRef.current) {
        try {
          janusRef.current.destroy({ cleanupHandles: true });
        } catch (destroyError) {
          console.warn('Janus destroy error:', destroyError);
        }
        janusRef.current = null;
      }
      if (videoRef.current) {
        videoRef.current.srcObject = null;
      }
    };
  }, [carId]);

  useEffect(() => {
    if (!pluginHandle) {
      return;
    }

    let cancelled = false;

    const updateStats = async () => {
      const handle = pluginHandleRef.current;
      if (!handle || cancelled) {
        return;
      }

      if (typeof handle.getBitrate === 'function') {
        const raw = handle.getBitrate();
        const parsed = parseBitrate(raw);
        if (!cancelled && parsed !== null) {
          setBitrate(prev => {
            if (prev === null || Math.abs(prev - parsed) > 500) {
              return parsed;
            }
            return prev;
          });
        }
      }

      const pc = (handle as unknown as { webrtcStuff?: { pc?: RTCPeerConnection } }).webrtcStuff?.pc;
      if (!pc || typeof pc.getStats !== 'function') {
        return;
      }

      try {
        const stats = await pc.getStats();
        if (cancelled) return;

        let fpsValue: number | null = null;
        stats.forEach((report: any) => {
          if (!report) {
            return;
          }
          if ((report.type === 'inbound-rtp' || report.type === 'track') && report.kind === 'video') {
            if (typeof report.framesPerSecond === 'number') {
              fpsValue = report.framesPerSecond;
            } else if (typeof report.framesDecoded === 'number' && typeof report.totalDecodeTime === 'number' && report.totalDecodeTime > 0) {
              fpsValue = report.framesDecoded / report.totalDecodeTime;
            }
          }
        });

        if (fpsValue === null) {
          const stream = videoRef.current?.srcObject;
          const track = stream instanceof MediaStream ? stream.getVideoTracks()[0] : undefined;
          const trackFps = track?.getSettings().frameRate;
          if (typeof trackFps === 'number' && Number.isFinite(trackFps)) {
            fpsValue = trackFps;
          }
        }

        if (!cancelled && fpsValue !== null) {
          setFps(prev => {
            if (prev === null || Math.abs(prev - fpsValue!) > 0.5) {
              return fpsValue!;
            }
            return prev;
          });
        }
      } catch (statsError) {
        console.debug('Janus stats error:', statsError);
      }
    };

    updateStats();
    const intervalId = window.setInterval(updateStats, 2000);

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, [pluginHandle]);

  const formattedBitrate = formatBitrate(bitrate);
  const formattedFps = typeof fps === 'number' && Number.isFinite(fps) ? `${fps.toFixed(1)} fps` : '--';

  return (
    <div className="relative w-full">
      {overlayVisible && (
        <div className="absolute top-2 left-2 bg-black/70 text-white px-3 py-2 rounded text-sm z-10 space-y-1">
          <div className="flex items-start justify-between gap-4">
            <div>
              Status: {streamStatus}
              {carId !== undefined && ` (Car ID: ${carId})`}
            </div>
            <button
              type="button"
              className="text-white/70 hover:text-white text-xs"
              onClick={() => setOverlayVisible(false)}
              aria-label="Overlay schließen"
            >
              ✕
            </button>
          </div>
          <div className="text-xs text-white/80">
            Stream: {formattedFps} | {formattedBitrate}
          </div>
        </div>
      )}
      {!overlayVisible && (
        <button
          type="button"
          className="absolute top-2 left-2 bg-black/60 text-white text-xs px-2 py-1 rounded z-10"
          onClick={() => setOverlayVisible(true)}
        >
          Info anzeigen
        </button>
      )}
      {error && (
        <div className="absolute top-10 left-2 bg-red-600/90 text-white px-3 py-1 rounded text-sm z-10">
          Error: {error}
        </div>
      )}
      <video
        ref={videoRef}
        autoPlay
        playsInline
        muted
        className="w-full h-auto bg-black"
        style={{ maxHeight: '80vh' }}
      />
    </div>
  );
}
