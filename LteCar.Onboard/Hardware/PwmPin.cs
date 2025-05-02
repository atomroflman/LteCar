using System.Diagnostics;

namespace LteCar.Onboard.Hardware;

public class PwmPin : BasePin
{
    private Process? _bash;

    public PwmPin(int pinNumber) : base(pinNumber)
    {
        var bashStart = new ProcessStartInfo("bash") {
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        _bash = Process.Start(bashStart);
        if (_bash == null)
            throw new Exception("Cannot start bash.");
        _bash.StandardInput.WriteLine($"gpio -g mode {pinNumber} pwm");
        _bash.StandardInput.WriteLine("gpio pwm-ms");
        _bash.StandardInput.WriteLine("gpio pwmc 192");
        _bash.StandardInput.WriteLine("gpio pwmr 2000");
    }

    public void SetPwmValue(int value)
    {
        _bash?.StandardInput.WriteLine($"gpio -g pwm {PinNumber} {value}");
        Console.WriteLine($"gpio -g pwm {PinNumber} {value}");
    }
}
