"use client";

import React, { useEffect, useState } from "react";

type OnboardInstallCommandResponse = {
  command?: string;
  installCommand?: string;
  scriptUrl: string;
  serverName: string;
  serverPort: number;
  useHttps: boolean;
  serverUrl: string;
  branch: string;
  gitRef?: string | null;
};

export default function InstallDialog() {
  const [isOpen, setIsOpen] = useState(false);
  const [installCommand, setInstallCommand] = useState<string>("");
  const [scriptUrl, setScriptUrl] = useState<string>("");
  const [branch, setBranch] = useState<string>("");
  const [gitRef, setGitRef] = useState<string | null>(null);
  const [isLoadingCommand, setIsLoadingCommand] = useState(false);
  const [commandError, setCommandError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const getInstallCommandEndpoints = () => {
    if (typeof window === "undefined") {
      return ["/api/install/onboard-command"];
    }

    const endpoints = ["/api/install/onboard-command"];
    const { protocol, hostname, port } = window.location;

    if (port && port !== "5000") {
      endpoints.push(`${protocol}//${hostname}:5000/api/install/onboard-command`);
    }

    return endpoints;
  };

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const abortController = new AbortController();

    const loadCommand = async () => {
      setIsLoadingCommand(true);
      setCommandError(null);
      setInstallCommand("");
      setScriptUrl("");
      setBranch("");
      setGitRef(null);

      try {
        let data: OnboardInstallCommandResponse | null = null;
        let lastError: Error | null = null;

        for (const endpoint of getInstallCommandEndpoints()) {
          try {
            const response = await fetch(endpoint, {
              cache: "no-store",
              signal: abortController.signal,
            });
            if (!response.ok) {
              throw new Error(`Install endpoint returned ${response.status}.`);
            }

            const json = await response.json() as OnboardInstallCommandResponse;
            const resolvedCommand = json.command ?? json.installCommand ?? "";
            if (!resolvedCommand || !json.scriptUrl) {
              throw new Error("Install endpoint returned an incomplete payload.");
            }

            data = json;
            break;
          } catch (error) {
            lastError = error instanceof Error ? error : new Error("Failed to load install command.");
          }
        }

        if (!data) {
          throw lastError ?? new Error("Failed to load install command.");
        }

        if (abortController.signal.aborted) {
          return;
        }

        setInstallCommand(data.command ?? data.installCommand ?? "");
        setScriptUrl(data.scriptUrl);
        setBranch(data.branch);
        setGitRef(data.gitRef ?? null);
      } catch (error) {
        if (!abortController.signal.aborted) {
          setCommandError(error instanceof Error ? error.message : "Failed to load install command.");
        }
      } finally {
        if (!abortController.signal.aborted) {
          setIsLoadingCommand(false);
        }
      }
    };

    void loadCommand();

    return () => {
      abortController.abort();
    };
  }, [isOpen]);

  const handleCopy = async () => {
    if (!installCommand) {
      return;
    }

    try {
      await navigator.clipboard.writeText(installCommand);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1800);
    } catch {
      setCommandError("Failed to copy command to clipboard.");
    }
  };

  return (
    <>
      <button
        type="button"
        className="inline-flex items-center justify-center rounded-full border border-green-500/45 bg-transparent px-3 py-2 text-[11px] font-medium text-green-300 shadow-sm transition hover:bg-green-500/10 hover:text-green-200"
        onClick={() => setIsOpen(true)}
      >
        Install New Vehicle
      </button>

      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 p-4 backdrop-blur-sm">
          <div className="relative w-full max-w-3xl overflow-hidden border border-white/8 bg-[linear-gradient(180deg,rgba(20,24,32,0.98),rgba(13,16,22,0.97))] p-6 text-slate-100 shadow-[0_30px_90px_rgba(0,0,0,0.45)]">
            <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-sky-400/70 to-transparent" />

            <button
              type="button"
              className="absolute right-4 top-4 inline-flex h-10 w-10 items-center justify-center rounded-full border border-white/10 bg-slate-900/70 text-slate-400 transition hover:border-white/20 hover:text-slate-100"
              onClick={() => setIsOpen(false)}
              aria-label="Close install dialog"
            >
              ×
            </button>

            <div className="space-y-5 pr-10">
              <div className="space-y-3">
                <span className="inline-flex rounded-full border border-sky-500/25 bg-sky-500/10 px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.3em] text-sky-300">
                  Onboard Install
                </span>
                <div className="space-y-2">
                  <h2 className="text-2xl font-semibold tracking-tight text-slate-100">Install Signal-RC on your car</h2>
                  <p className="max-w-lg text-sm leading-6 text-slate-400">
                    Load the preconfigured onboard installer command from this controller, copy it, and run it directly on the vehicle shell.
                  </p>
                </div>
              </div>

              <div className="space-y-4">
                <div className="border border-white/8 bg-slate-900/55 p-4 shadow-[0_14px_32px_rgba(0,0,0,0.22)]">
                  <div className="mb-3 text-xs font-semibold uppercase tracking-[0.22em] text-slate-400">Run this on the vehicle</div>
                  <div className="overflow-hidden rounded-xl border border-white/10 bg-[#0d1117] shadow-[inset_0_1px_0_rgba(255,255,255,0.04)]">
                    <div className="flex items-center justify-between border-b border-white/6 px-3 py-2">
                      <div className="text-xs font-medium text-slate-400">Shell</div>
                      <button
                        type="button"
                        className="inline-flex h-9 w-9 items-center justify-center rounded-md border border-white/10 bg-transparent text-slate-300 transition hover:border-white/20 hover:bg-white/5 hover:text-slate-100"
                        onClick={() => void handleCopy()}
                        aria-label="Copy install command"
                        title={copied ? "Copied" : "Copy command"}
                      >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                          <path d="M6 2.75C6 2.33579 6.33579 2 6.75 2H12.25C12.6642 2 13 2.33579 13 2.75V9.25C13 9.66421 12.6642 10 12.25 10H11V4.75C11 4.33579 10.6642 4 10.25 4H6V2.75Z" stroke="currentColor" strokeWidth="1.2"/>
                          <rect x="3" y="5" width="8" height="9" rx="0.75" stroke="currentColor" strokeWidth="1.2"/>
                        </svg>
                      </button>
                    </div>

                    <div className="flex items-center gap-3 px-4 py-4 font-mono text-[15px] leading-7 text-slate-200">
                      <span className="shrink-0 text-slate-500">$</span>
                      {isLoadingCommand ? (
                        <span className="text-slate-500">Loading install command...</span>
                      ) : commandError ? (
                        <span className="text-red-300">{commandError}</span>
                      ) : (
                        <span className="break-all">{installCommand}</span>
                      )}
                    </div>
                  </div>

                  {copied && <div className="mt-2 text-xs text-green-300">Command copied.</div>}
                </div>

                <div className="grid gap-4 md:grid-cols-[1.1fr_0.9fr]">
                  <div className="border border-white/8 bg-slate-900/55 p-4 shadow-[0_14px_32px_rgba(0,0,0,0.22)]">
                    <div className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-400">Steps</div>
                    <ol className="mt-4 space-y-3 text-sm text-slate-300">
                      <li className="flex gap-3 leading-6">
                        <span className="mt-0.5 inline-flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-sky-500/15 text-[11px] font-semibold text-sky-300 ring-1 ring-sky-500/20">1</span>
                        <span>Open a shell on the Raspberry Pi or onboard Linux host.</span>
                      </li>
                      <li className="flex gap-3 leading-6">
                        <span className="mt-0.5 inline-flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-sky-500/15 text-[11px] font-semibold text-sky-300 ring-1 ring-sky-500/20">2</span>
                        <span>Paste the command exactly as shown and run it with sudo.</span>
                      </li>
                      <li className="flex gap-3 leading-6">
                        <span className="mt-0.5 inline-flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-sky-500/15 text-[11px] font-semibold text-sky-300 ring-1 ring-sky-500/20">3</span>
                        <span>Follow the interactive installer to finish onboard setup and service installation.</span>
                      </li>
                    </ol>
                  </div>

                  <div className="border border-sky-500/18 bg-[linear-gradient(180deg,rgba(14,165,233,0.10),rgba(15,23,42,0.22))] p-4">
                    <div className="text-xs font-semibold uppercase tracking-[0.22em] text-sky-300">Install Source</div>
                    <div className="mt-4 space-y-3 text-sm text-slate-300">
                      <div>
                        <div className="text-[11px] uppercase tracking-[0.18em] text-slate-500">Endpoint</div>
                        <div className="mt-1 break-all font-mono text-[13px] text-slate-200">
                          {isLoadingCommand ? "Loading source..." : scriptUrl || "Unavailable"}
                        </div>
                      </div>
                      <div className="grid grid-cols-2 gap-3">
                        <div>
                          <div className="text-[11px] uppercase tracking-[0.18em] text-slate-500">Branch</div>
                          <div className="mt-1 text-slate-200">{isLoadingCommand ? "Loading..." : branch || "Unavailable"}</div>
                        </div>
                        <div>
                          <div className="text-[11px] uppercase tracking-[0.18em] text-slate-500">Git Ref</div>
                          <div className="mt-1 break-all font-mono text-[13px] text-slate-200">{isLoadingCommand ? "Loading..." : gitRef || "Unavailable"}</div>
                        </div>
                      </div>
                    </div>

                    <div className="mt-4 flex flex-col gap-3">
                      <button
                        type="button"
                        className="inline-flex items-center justify-center border border-white/10 bg-transparent px-4 py-3 text-sm font-semibold text-slate-200 transition hover:border-white/20 hover:bg-white/5"
                        onClick={() => setIsOpen(false)}
                      >
                        Continue to control view
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}