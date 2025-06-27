using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Hardware;

public class PwmPin : BasePin, IPwmModule
{
    private Process? _bash;
    private readonly ILogger<PwmPin> _logger;
    private float _lastValue = 0;

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

    public void SetPwmValue(float value)
    {
        if (value < 0f) value = 0f;
        if (value > 1f) value = 1f;
        _lastValue = value;
        int pwmValue = (int)Math.Round(50 + value * (250 - 50)); // 50 = aus, 250 = voll an
        WriteBash($"gpio -g pwm {PinNumber} {pwmValue}");
    }

    public float GetPwmValue() => _lastValue;

    private void WriteBash(string cmd) {
        _logger.LogDebug($"Writing bash {{_bash?.Id}}: {cmd}");
        _bash?.StandardInput.WriteLine(cmd);
    }
}
