using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Hardware;

public class RaspberryPiPwmPin : IPwmModule
{
    private readonly ILogger<RaspberryPiPwmPin> _logger;
    private float _lastValue = 0;
    private bool _initialized = false;

    public Bash Bash { get; }
    public int PinNumber { get; }

    public RaspberryPiPwmPin(IServiceProvider serviceProvider, int pinNumber)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<RaspberryPiPwmPin>>();
        Bash = serviceProvider.GetRequiredService<Bash>();
        PinNumber = pinNumber;
    }

    private async Task InitializePin(int pinNumber)
    {
        if (_initialized)
            return;
        _initialized = true;
        _logger.LogInformation($"Initializing PWM pin {pinNumber}");
        await Bash.ExecuteAsync($"gpio -g mode {pinNumber} pwm");
        await Bash.ExecuteAsync("gpio pwm-ms");
        await Bash.ExecuteAsync("gpio pwmc 192");
        await Bash.ExecuteAsync("gpio pwmr 2000");
        await SetPwmValue(0f); // Set initial value to 0
    }

    public async Task SetPwmValue(float value)
    {
        if (!_initialized)
            await InitializePin(PinNumber);
        if (value < 0f) value = 0f;
        if (value > 1f) value = 1f;
        _lastValue = value;
        int pwmValue = (int)Math.Round(50 + value * (250 - 50));
        await Bash.ExecuteAsync($"gpio -g pwm {PinNumber} {pwmValue}");
    }

    public Task<float> GetPwmValue() => Task.FromResult(_lastValue);
}
