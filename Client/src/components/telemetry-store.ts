import { create } from "zustand";
import type { HubConnection } from "@microsoft/signalr";

export interface TelemetryEntry {
  name: string;
  value: string;
  updatedAt: number;
}

export interface TelemetryState {
  entries: TelemetryEntry[];
  isConnected: boolean;
  subscribedCarId: string | null;
  subscribe: (carId: number) => Promise<void>;
  unsubscribe: () => Promise<void>;
}

let _connection: HubConnection | null = null;

async function ensureConnection(
  set: (partial: Partial<TelemetryState>) => void,
  get: () => TelemetryState
): Promise<HubConnection> {
  if (_connection) return _connection;

  const signalR = await import("@microsoft/signalr");
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/telemetry")
    .withAutomaticReconnect()
    .build();

  connection.on("UpdateTelemetry", (valueName: string, value: string) => {
    const entry: TelemetryEntry = { name: valueName, value, updatedAt: Date.now() };
    set({
      entries: upsertEntry(get().entries, entry),
    });
  });

  connection.onclose(() => set({ isConnected: false }));

  connection.onreconnected(async () => {
    set({ isConnected: true });
    const { subscribedCarId } = get();
    if (subscribedCarId) {
      await connection.invoke("SubscribeToCarTelemetry", subscribedCarId);
    }
  });

  await connection.start();
  _connection = connection;
  set({ isConnected: true });
  return connection;
}

function upsertEntry(entries: TelemetryEntry[], entry: TelemetryEntry): TelemetryEntry[] {
  const idx = entries.findIndex((e) => e.name === entry.name);
  if (idx !== -1) {
    const updated = [...entries];
    updated[idx] = entry;
    return updated;
  }
  return [...entries, entry];
}

export const useTelemetryStore = create<TelemetryState>((set, get) => ({
  entries: [],
  isConnected: false,
  subscribedCarId: null,

  async subscribe(carId: number) {
    const state = get();
    const carIdStr = carId.toString();

    if (state.subscribedCarId === carIdStr) return;

    const connection = await ensureConnection(set, get);

    if (state.subscribedCarId) {
      await connection
        .invoke("UnsubscribeFromCarTelemetry", state.subscribedCarId)
        .catch(() => {});
    }

    set({ entries: [], subscribedCarId: carIdStr });
    await connection.invoke("SubscribeToCarTelemetry", carIdStr);
  },

  async unsubscribe() {
    const { subscribedCarId } = get();
    if (_connection && subscribedCarId) {
      await _connection
        .invoke("UnsubscribeFromCarTelemetry", subscribedCarId)
        .catch(() => {});
    }
    if (_connection) {
      await _connection.stop().catch(() => {});
      _connection = null;
    }
    set({ entries: [], isConnected: false, subscribedCarId: null });
  },
}));
