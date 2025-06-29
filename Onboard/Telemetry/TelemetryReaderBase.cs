using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Telemetry;

public abstract class TelemetryReaderBase : IDisposable
{
    protected readonly ILogger Logger;
    public int ReadIntervalTicks { get; set; } = 1;

    public TelemetryReaderBase(ILogger logger)
    {
        Logger = logger;
    }

    public abstract Task<string> ReadTelemetry();

    public void Dispose()
    {
    }
}
