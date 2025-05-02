using System.Device.Gpio;
using System.Device.Pwm;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi.Native;

namespace LteCar.Onboard.Control.ControlTypes;

public abstract class ControlTypeBase : IDisposable
{
    public abstract PinFunctionFlags RequiredFunctions { get; }
    public int Pin { get; set; }
    public string Name { get; set; } = string.Empty;
    public abstract void OnControlRecived(decimal newValue);
    public abstract void OnControlReleased();
    public abstract void Start();
    public abstract void Dispose();
}

public class ControlTypeAttribute : Attribute
{
    public string TypeName { get; }
    
    public ControlTypeAttribute(string typeName)
    {
        TypeName = typeName;
    }
}

public abstract class ServoControlBase : ControlTypeBase
{
    public override PinFunctionFlags RequiredFunctions => PinFunctionFlags.PWM;
    
    
    public override void Start()
    {
    }

    public override void OnControlRecived(decimal newValue)
    {
        WiringPi.PwmWrite(Pin, ScaleRangeToPwm(newValue));
        
    }

    public override void OnControlReleased()
    {
        OnControlRecived(0);
    }
    
    /// <summary>
    /// Scales a value in the range of -1 to 1 to a PWM value (e.g., 1000-2000 µs).
    /// </summary>
    /// <param name="scaledValue">The value to scale, in the range of -1 to 1.</param>
    /// <param name="minPwm">The minimum PWM value (e.g., 1000).</param>
    /// <param name="maxPwm">The maximum PWM value (e.g., 2000).</param>
    /// <returns>The corresponding PWM value.</returns>
    public int ScaleRangeToPwm(decimal scaledValue, decimal minPwm = 1000, decimal maxPwm = 2000)
    {
        decimal midPwm = (minPwm + maxPwm) / 2;
        decimal range = (maxPwm - minPwm) / 2;
        return (int)Math.Round(scaledValue * range + midPwm, 0);
    }
}
