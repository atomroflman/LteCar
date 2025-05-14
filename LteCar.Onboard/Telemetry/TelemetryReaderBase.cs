using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Telemetry;

public abstract class TelemetryReaderBase
{
    protected readonly ILogger Logger;
    public int ReadIntervalTicks { get; set; } = 1;

    public TelemetryReaderBase(ILogger logger)
    {
        Logger = logger;
    }

    public abstract void ReadTelemetry();
}
