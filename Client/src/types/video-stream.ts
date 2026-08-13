// ponytail: server's GetVideoStreamsForCar returns VideoStreamMapItem (serverId),
// not VideoStreamInfoModel (id) anymore. Match the server.
export type VideoStreamInfo = {
  serverId: number;
  name: string;
  streamId: string;
  type: string;
  location?: string | null;
  width: number;
  height: number;
  bitrateKbps: number;
  framerate: number;
  brightness: number;
  gain?: number | null;
  shutter?: number | null;
  contrast?: number | null;
  ev?: number | null;
  exposure?: string | null;
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
  gain?: number | null;
  shutter?: number | null;
  contrast?: number | null;
  ev?: number | null;
  exposure?: string | null;
};