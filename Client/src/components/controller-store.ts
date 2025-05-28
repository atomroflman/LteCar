import { create } from "zustand";

export type UserGamepadCreateRequest = {
    id: string;
    axes: number;
    buttons: number;
    registered: boolean;
};

export type UserGamepadDatabase = {
    id: number;
    name: string;
    axes: GamepadAxisChannel[];
    buttons: GamepadChannel[];
    connected: boolean; // Indicates if the gamepad is currently connected
};

export type GamepadChannel = {
    id: number;
    name: string;
    latestValue: number;
    channelId: number;
}

export type GamepadAxisChannel = GamepadChannel & {
    calibrationMin: number;
    calibrationMax: number;
    accuracy: number; // Number of decimal places for this axis
}

function initLatestValues(gp: UserGamepadDatabase): UserGamepadDatabase {
    if (!gp?.axes || !gp?.buttons) {
        debugger;
    }
    const axes = gp.axes.sort((a, b) => a.channelId - b.channelId);
    axes.forEach((axis, i) => {
        axis.latestValue = 0; // Initialize latest value for axes
    });
    const buttons = gp.buttons.sort();
    buttons.forEach((button, i) => {
        button.latestValue = 0; // Initialize latest value for buttons
    });
    return {
        id: gp.id,
        name: gp.name,
        axes: axes,
        buttons: buttons,
        connected: false
    };
}

export type GamepadStoreState = {
    gamepadsLoaded: boolean;
    pollFps: number;
    setPollFps: (fps: number) => void;
    knownGamepads: Record<string, UserGamepadDatabase>;
    setChannelAccuracy: (gamepadId: string, channelIndex: number, accuracy: number) => void;
    loadInitialGamepads: () => Promise<void>;
    pollGamepads: () => void;
    stopPolling: () => void;
    onChannelChange?: (gamepadId: string, channelType: "axis" | "button", channelIndex: number, value: number) => void;
    setOnChannelChange: (cb: (gamepadId: string, channelType: "axis" | "button", channelIndex: number, value: number) => void) => void;
};

export const useGamepadStore = create<GamepadStoreState>((set, get) => {
    let pollReference: number | undefined;
    let lastValues: Record<string, { axes: number[]; buttons: number[] }> = {};
    return {
        knownGamepads: {},
        gamepadsLoaded: false,
        onChannelChange: undefined,
        pollFps: 15,
        setChannelAccuracy: (gamepadId: string, channelIndex: number, accuracy: number) => {
            const known = get().knownGamepads;
            if (known[gamepadId]) {
                const axis = known[gamepadId].axes.find(a => a.channelId === channelIndex);
                if (axis) {
                    axis.accuracy = accuracy;
                    set({ knownGamepads: { ...known } });
                }
            }
        },
        setOnChannelChange: (cb) => set({ onChannelChange: cb }),
        async loadInitialGamepads() {
            // Load already registered gamepads for this user from backend
            const res = await fetch(`/api/userconfig/gamepads`);
            if (res.ok) {
                const data = await res.json();
                const mapped: Record<string, UserGamepadDatabase> = {};
                for (const g of data) {
                    mapped[g.name] = initLatestValues(g);
                }
                set({ knownGamepads: mapped, gamepadsLoaded: true });
            } else {
                console.error("Failed to load gamepads from backend", res.status, res.statusText);
                get().setPollFps(0.1);
            }
        },
        setPollFps: (fps: number) => {
            set({ pollFps: fps });
            if (pollReference) {
                clearInterval(pollReference);
                pollReference = undefined;
            }
            if (fps > 0) {
                pollReference = window.setInterval(() => {
                    get().pollGamepads();
                }, 1000 / fps);
            }
        },
        async pollGamepads() {
            const gps = navigator.getGamepads() as Gamepad[];
            const known = get().knownGamepads;
            for (const gp of gps) {
                if (!gp) continue;
                const last = lastValues[gp.id];
                const axes = Array.from(gp.axes);
                const buttons = gp.buttons.map(b => b.value);

                if (!known[gp.id]) {
                    // New gamepad detected, register it
                    const payload = {
                        deviceName: gp.id,
                        axes: gp.axes.length,
                        buttons: gp.buttons.length,
                    };
                    const res = await fetch(`/api/userconfig/register-gamepad`, {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(payload),
                    });
                    if (res.ok) {
                        const data = await res.json() as UserGamepadDatabase;
                        known[data.name] = initLatestValues(data);
                    } else {
                        console.error("Failed to load gamepads from backend", res.status, res.statusText);
                        get().setPollFps(0.1);
                    }
                } else if (!known[gp.id].connected) {
                    // Gamepad was previously registered but not connected
                    known[gp.id].connected = true;
                }

                if (!last) {
                    axes.forEach((val, i) => {
                        if (typeof get().onChannelChange === 'function')
                            get().onChannelChange!(gp.id, "axis", i, val);
                    });
                    buttons.forEach((val, i) => {
                        if (typeof get().onChannelChange === 'function')
                            get().onChannelChange!(gp.id, "button", i, val);
                    });
                } else {
                    axes.forEach((val, i) => {
                        if (val !== last.axes[i]) {
                            if (typeof get().onChannelChange === 'function')
                                get().onChannelChange!(gp.id, "axis", i, val);
                        }
                    });
                    buttons.forEach((val, i) => {
                        if (val !== last.buttons[i]) {
                            if (typeof get().onChannelChange === 'function')
                                get().onChannelChange!(gp.id, "button", i, val);
                        }
                    });
                }
                lastValues[gp.id] = { axes, buttons };

            }
            set({ knownGamepads: known });
        },
        stopPolling() {
            if (pollReference) {
                clearInterval(pollReference);
                pollReference = undefined;
            }
        },
    };
});
