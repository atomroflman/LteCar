-- =====================================================
-- 1. CREATE DUMMY USER
-- =====================================================
INSERT OR IGNORE INTO Users (Id, Name, ActiveVehicleId, SessionToken, LastSeen)
VALUES (1, 'Dummy User', NULL, 'dummy-session-token-12345', datetime('now'));

-- =====================================================
-- 2. CREATE DUMMY CAR
-- =====================================================
INSERT INTO Cars (Id, Name, CarId, ChannelMapHash, VideoStreamPort, LastSeen)
VALUES (
    1, 
    'Dummy RC Car', 
    'DUMMY-CAR-001', 
    'dummy-channel-map-hash-12345', 
    8080, 
    datetime('now')
);

-- =====================================================
-- 3. CREATE CONTROL CHANNELS
-- =====================================================
-- Steering Channel
INSERT INTO CarChannels (Id, DisplayName, ChannelName, IsEnabled, RequiresAxis, CarId)
VALUES (1, 'Steering', 'steer', 1, 1, 1);

-- Throttle Channel
INSERT INTO CarChannels (Id, DisplayName, ChannelName, IsEnabled, RequiresAxis, CarId)
VALUES (2, 'Throttle', 'throttle', 1, 1, 1);

-- Gear Up Channel
INSERT INTO CarChannels (Id, DisplayName, ChannelName, IsEnabled, RequiresAxis, CarId)
VALUES (3, 'Gear Up', 'gearUp', 1, 0, 1);

-- Gear Down Channel
INSERT INTO CarChannels (Id, DisplayName, ChannelName, IsEnabled, RequiresAxis, CarId)
VALUES (4, 'Gear Down', 'gearDown', 1, 0, 1);

-- Lights Channel
INSERT INTO CarChannels (Id, DisplayName, ChannelName, IsEnabled, RequiresAxis, CarId)
VALUES (5, 'Lights', 'lights', 1, 0, 1);

-- Horn Channel
INSERT INTO CarChannels (Id, DisplayName, ChannelName, IsEnabled, RequiresAxis, CarId)
VALUES (6, 'Horn', 'horn', 1, 0, 1);

-- =====================================================
-- 4. CREATE TELEMETRY CHANNELS
-- =====================================================
-- CPU Temperature
INSERT INTO CarTelemetry (Id, ChannelName, CarId, ReadIntervalTicks, TelemetryType)
VALUES (1, 'CpuTemperature', 1, 1000, 'CpuTemperatureReader');

-- Battery Level
INSERT INTO CarTelemetry (Id, ChannelName, CarId, ReadIntervalTicks, TelemetryType)
VALUES (2, 'BatteryLevel', 1, 2000, 'JbdBmsTelemetryReader');

-- =====================================================
-- 5. CREATE VIDEO STREAMS
-- =====================================================
-- Main Camera
INSERT INTO CarVideoStreams (
    Id, StreamId, CarId, Protocol, Port, StartTime, IsActive, 
    ProcessArguments, StreamPurpose, Name, Description, Type, 
    Location, Priority, Enabled, LastStatusUpdate
)
VALUES (
    1, 'main-camera', 1, 'UDP', 8080, datetime('now'), 1,
    'libcamera-vid --width 1920 --height 1080 --framerate 30 --codec yuv420',
    'video', 'Main Camera', 'Primary vehicle camera for driving', 'camera',
    'front', 1, 1, datetime('now')
);

-- Rear Camera
INSERT INTO CarVideoStreams (
    Id, StreamId, CarId, Protocol, Port, StartTime, IsActive, 
    ProcessArguments, StreamPurpose, Name, Description, Type, 
    Location, Priority, Enabled, LastStatusUpdate
)
VALUES (
    2, 'rear-camera', 1, 'UDP', 8081, datetime('now'), 1,
    'libcamera-vid --width 1280 --height 720 --framerate 25 --codec yuv420',
    'video', 'Rear Camera', 'Rear view camera for parking assistance', 'camera',
    'rear', 2, 1, datetime('now')
);

