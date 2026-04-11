# DebugDesktop

Minimal debug configuration for running the Onboard client on a non-Pi machine.

## Contains

- No control channels
- One telemetry channel: `cpuTemperature`
- No video streams

## Start

From `Onboard/`:

```bash
dotnet run -- --config-dir=../Builds/DebugDesktop
```

## Purpose

This config avoids Raspberry Pi-only hardware so you can run server and client locally for testing.
