"use client";

import React, {useEffect, useRef, useState} from "react";
//import "bootstrap/dist/css/bootstrap.min.css";
//import "font-awesome/css/font-awesome.min.css";
// import Janus from "janus-gateway";
import Script from "next/script";


interface PluginHandle {
  send: (message: any) => void;
  createAnswer: (options: {
    jsep: any;
    media: { audioSend: boolean; videoSend: boolean };
    success: (jsep: any) => void;
    error: (error: Error) => void;
  }) => void;
}

declare var Janus: any | undefined;

export default function VideoStream(): React.ReactElement {
  const videoRef = useRef<HTMLVideoElement>(null);
  // const [janus, setJanus] = useState<Janus | null>(null);
  const [pluginHandle, setPluginHandle] = useState<PluginHandle | null>(null);

  const loadJanusScript = (): Promise<void> => {
    return new Promise((resolve, reject) => {
      if (typeof Janus !== "undefined") {
        resolve();
        return;
      }

      const script = document.createElement("script");
      script.src = "https://cdn.jsdelivr.net/npm/janus-gateway@latest";
      script.async = true;
      script.onload = () => {
        console.log("Janus script loaded");
        resolve();
      };
      script.onerror = () => {
        reject(new Error("Failed to load Janus script"));
      };
      document.body.appendChild(script);
    });
  };

  useEffect(() => {
    const loadJanus = async () => {
      
      await loadJanusScript(); // Warte, bis das Skript geladen ist
      if (typeof Janus === "undefined") {
        console.error("Janus is still undefined after script load");
        return;
      }

      console.log("Janus is available:", Janus);

      try {
        Janus.init({
          debug: "all",
          callback: () => {
            const janusInstance = new Janus({
              server: "ws://192.168.3.149:8188",
              success: () => {
                if (!janusInstance || !janusInstance.attach) return;
                janusInstance.attach({
                  plugin: "janus.plugin.streaming",
                  success: (handle: any) => {
                    setPluginHandle(handle);
                    console.log("Plugin attached:", handle);
                    handle.send({ message: { request: "list" } });
                  },
                  error: (error: string) => {
                    console.error("Error attaching plugin:", error);
                  },
                  onmessage: (msg: any, jsep: any) => {
                    if (msg.result && msg.result.list) {
                      console.log("Available streams:", msg.result.list);
                      const streamId = msg.result.list[0]?.id;
                      if (streamId && pluginHandle) {
                        pluginHandle.send({ message: { request: "watch", id: streamId } });
                      }
                    }
                    if (jsep && pluginHandle) {
                      pluginHandle.createAnswer({
                        jsep,
                        media: { audioSend: false, videoSend: false },
                        success: (jsep) => {
                          pluginHandle.send({ message: { request: "start" }, jsep });
                        },
                        error: (error: Error) => {
                          console.error("Error creating answer:", error);
                        },
                      });
                    }
                  },
                  onremotetrack: (stream: any) => {
                    console.log("Remote stream received:", stream);
                    if (videoRef.current) {
                      videoRef.current.srcObject = stream;
                    }
                  },
                  oncleanup: () => {
                    console.log("Plugin cleaned up");
                  },
                });
              },
              error: (error: Error) => {
                console.error("Janus initialization error:", error);
              },
              destroyed: () => {
                console.log("Janus instance destroyed");
              },
            });
            // setJanus(janusInstance);
          },
        });
      } catch (error) {
        console.error("Error loading Janus.js:", error);
      }
    };

    loadJanus();

    // return () => {
    //   if (janus) {
    //     janus.destroy({ });
    //   }
    // };
  }, [pluginHandle]);

  return (
    <>

<Script
        src="https://cdn.jsdelivr.net/npm/janus-gateway@latest"
        strategy="beforeInteractive"
        onLoad={() => console.log("Janus script loaded")}
      />
      <div className="container mt-4">
        <h1>Janus Video Stream</h1>
        <div className="video-container">
          <video
              ref={videoRef}
              autoPlay
              playsInline
              controls
              style={{ width: "100%", height: "auto", backgroundColor: "black" }}
          />
        </div>
      </div>
      </>
  );
};