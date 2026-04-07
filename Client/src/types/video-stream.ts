export type VideoStreamInfo = {
  id: number;
  name: string;
  streamId: string;
  type: string;
  location?: string | null;
  priority: number;
  width: number;
  height: number;
  bitrateKbps: number;
  framerate: number;
  brightness: number;
  enabled: boolean;
  isActive: boolean;
  viewerCount: number;
};

export type VideoSettingsPayload = {
  width: number;
  height: number;
  framerate: number;
  bitrateKbps: number;
  brightness: number;
};