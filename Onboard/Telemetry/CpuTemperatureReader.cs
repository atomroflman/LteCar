using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Telemetry
{
    public class CpuTemperatureReader : TelemetryReaderBase
    {
        public CpuTemperatureReader(ILogger logger) : base(logger) { }

        public override async Task<string> ReadTelemetry()
        {
            try
            {
                // Standard location for CPU temp on Raspberry Pi
                string path = "/sys/class/thermal/thermal_zone0/temp";
                if (!File.Exists(path))
                    return "not available";
                var content = await File.ReadAllTextAsync(path);
                if (int.TryParse(content.Trim(), out int tempMilliC))
                {
                    double tempC = tempMilliC / 1000.0;
                    Logger.LogDebug($"CPU Temperature: {tempC:F2}°C");
                    return $"{tempC:F2}°C";
                }
                return "parse error";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read CPU temperature");
                return "error";
            }
        }
    }
}
