using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Hardware;

public class PwmPin : BasePin
{
    private Process? _bash;
    private ILogger<PwmPin> _logger;

    public PwmPin(int pinNumber, IServiceProvider serviceProvider) : base(pinNumber, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<PwmPin>>();
        var bashStart = new ProcessStartInfo("bash") {
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        _bash = Process.Start(bashStart);
        if (_bash == null)
            throw new Exception("Cannot start bash.");
        WriteBash($"gpio -g mode {pinNumber} pwm");
        WriteBash("gpio pwm-ms");
        WriteBash("gpio pwmc 192");
        WriteBash("gpio pwmr 2000");
    }

    public void SetPwmValue(int value)
    {
        WriteBash($"gpio -g pwm {PinNumber} {value}");
    }

    private void WriteBash(string cmd) {
        _logger.LogDebug($"Writing bash {_bash?.Id}: {cmd}");
        _bash?.StandardInput.WriteLine(cmd);
    }
}
