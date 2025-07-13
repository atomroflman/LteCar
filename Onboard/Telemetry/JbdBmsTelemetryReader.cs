using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Telemetry;

public class JbdBmsTelemetryReader : TelemetryReaderBase
{
    private readonly Bash _bash;

    /// <summary>
    /// Gets or sets the channel to use for communication (e.g., serial port or Bluetooth address).
    /// </summary>
    public string Channel { get; set; }
    public string JbdToolPath { get; set; } = "~/ltecar/bin/jbdtools";

    public JbdBmsTelemetryReader(ILogger<JbdBmsTelemetryReader> logger, ILogger<Bash> bashLogger, Bash bash)
        : base(logger)
    {
        _bash = bash;

        // Check if jbdtool is installed
        var whichResult = _bash.ExecuteAndRead("which jbdtool")?.Trim();
        if (string.IsNullOrEmpty(whichResult) || !File.Exists(whichResult))
        {
            Logger.LogWarning("jbdtool CLI is not installed or not found in PATH. Please install jbdtools. (sudo apt install python3-venv python3-pip && python3 -m venv ~/ltecar && source ~/ltecar/bin/activate && pip install jbdtools)");
        }
    }

    public override async Task<string> ReadTelemetry()
    {
        // Use jbdtools to read BMS data via bash
        // Allow both serial and Bluetooth channels
        // Example: jbdtool --port /dev/ttyUSB0 status OR jbdtool --bluetooth XX:XX:XX:XX:XX:XX status
        string cmd;
        if (Channel.StartsWith("/dev/"))
        {
            cmd = $"{JbdToolPath} --port {Channel} status";
        }
        else
        {
            cmd = $"{JbdToolPath} --bluetooth {Channel} status";
        }
        Logger.LogDebug($"Running: {cmd}");
        string output = await Task.Run(() => _bash.ExecuteAndRead(cmd));
        Logger.LogInformation($"Received JBD BMS data: {output.Trim()}");
        return output.Trim();
    }
}
