'use client';

import { useEffect, useRef, useState } from 'react';

declare global {
  interface Window {
    Janus: any;
  }
}

export default function VideoStream() {
  const videoRef = useRef<HTMLVideoElement>(null);

  useEffect(() => {
    if (typeof window === 'undefined' || !window.Janus) return;

    window.Janus.init({
      debug: 'all',
      dependencies: window.Janus.useDefaultDependencies(),
      callback: () => {
        const janus = new window.Janus({
          server: ['http://ubuntu:8088/janus', 'ws://yourserver:8188/'],
          success: () => {
            let pluginHandle: any;
    
            janus.attach({
              plugin: 'janus.plugin.streaming',
              success: (handle: any) => {
                console.log("handle recieved:", handle);
                pluginHandle = handle;
                pluginHandle.send({ message: { request: 'list' } });
              },
              onmessage: (msg: any, jsep: any) => {
                console.log("Onmessge", msg, jsep);
                if (msg.streaming === 'list' && msg.list.length > 0) {
                  const streamId = msg.list[0].id; // nimm ersten verfügbaren Stream
                  console.log("Stream ID: ", streamId);
                  pluginHandle.send({ message: { request: 'watch', id: streamId } });
                }
    
                if (jsep) {
                  pluginHandle.createAnswer({
                    jsep,
                    media: { audioSend: false, videoSend: false },
                    success: (jsepAnswer: any) => {
                      pluginHandle.send({
                        message: { request: 'start' },
                        jsep: jsepAnswer,
                      });
                    },
                    error: (err: any) => {
                      console.error('createAnswer error', err);
                    },
                  });
                }
              },
              onremotestream: (stream: MediaStream) => {
                if (videoRef.current) {
                  window.Janus.attachMediaStream(videoRef.current, stream);
                }
              },
              error: (err: any) => {
                console.error('Plugin attach error:', err);
              },
            });
          },
          error: (err: any) => {
            console.error('Janus init error:', err);
          },
          destroyed: () => {
            console.log('Janus session destroyed');
          },
        });
      },
    });
  }, []);

  return (
    <div>
      <>Hier sollte video sein</>
      <video ref={videoRef} autoPlay playsInline muted style={{ width: '100%' }} />
    </div>
  );
}
