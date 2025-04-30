using Microsoft.Extensions.Logging;
using LteCar.Onboard.Hardware;

namespace LteCar.Onboard.Control.ControlTypes;

public abstract class ServoControlBase : ControlTypeBase
{
    PwmPin _pinInstance;
    public override PinFunctionFlags RequiredFunctions => PinFunctionFlags.PWM;

    public ILogger<ServoControlBase> Logger { get; }
    public PinManager PinManager { get; }

    public ServoControlBase(ILogger<ServoControlBase> logger, PinManager pinManager)
    {
        Logger = logger;
        PinManager = pinManager;
    }

    public override void Initialize()
    {
        base.Initialize();
        _pinInstance = PinManager.AllocatePin<PwmPin>(Pin);
    }
    
    public override void OnControlRecived(decimal newValue)
    {
        Logger.LogDebug($"{this.GetType().Name} rec: {newValue} pwm: {ScaleRangeToPwm(newValue)}");
        _pinInstance.SetPwmValue(ScaleRangeToPwm(newValue));
    }

    protected override async Task RunTestInternalAsync()
    {
        const int DELAY = 1000;
        for (int i = 0; i<3;i++) {
            OnControlRecived(-1);
            await Task.Delay(DELAY);
            OnControlRecived(1);
            await Task.Delay(DELAY);
            OnControlRecived(0);
            await Task.Delay(DELAY);
        }
        OnControlReleased();
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
    public int ScaleRangeToPwm(decimal scaledValue, decimal minPwm = 50, decimal maxPwm = 250)
    {
        decimal midPwm = (minPwm + maxPwm) / 2;
        decimal range = (maxPwm - minPwm) / 2;
        return (int)Math.Round(scaledValue * range + midPwm, 0);
    }
}