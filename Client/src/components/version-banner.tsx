"use client";

import { useEffect, useState } from "react";
import { useI18n } from "@/i18n/provider";

type VersionInfo = { branch: string; commit: string | null };
type VersionSync = {
  server: VersionInfo;
  onboard: VersionInfo | null;
  mismatch: boolean;
};

export default function VersionBanner({ carId }: { carId: number }) {
  const { messages } = useI18n();
  const t = messages.versionBanner;
  const [data, setData] = useState<VersionSync | null>(null);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const res = await fetch(`/api/cars/${carId}/version-sync`);
        if (!res.ok) return;
        const body = (await res.json()) as VersionSync;
        if (!cancelled) setData(body);
      } catch {
        // ponytail: silent retry, mismatch stays visible from last good response
      }
    };
    void load();
    const id = setInterval(load, 30_000);
    return () => {
      cancelled = true;
      clearInterval(id);
    };
  }, [carId]);

  if (!data) return null;

  if (!data.onboard) {
    return (
      <div className="border border-amber-700/60 bg-amber-900/30 px-3 py-2 text-xs text-amber-200">
        <span className="font-semibold">{t.title}: </span>
        {t.unknownOnboard}
      </div>
    );
  }

  if (!data.mismatch) return null;

  const s = data.server;
  const o = data.onboard;
  return (
    <div className="border border-red-700/60 bg-red-900/30 px-3 py-2 text-xs text-red-200">
      <div className="font-semibold">{t.title}</div>
      <div className="mt-1 opacity-90">
        {t.mismatchDetail(s.branch, s.commit ?? "?", o.branch, o.commit ?? "?")}
      </div>
      <div className="mt-1 opacity-80">{t.hint}</div>
    </div>
  );
}