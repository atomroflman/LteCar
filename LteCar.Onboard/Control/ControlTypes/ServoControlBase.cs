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
        _pinInstance = PinManager.AllocatePin<PwmPin>(Pin!.Value);
    }
    
    public override void OnControlRecived(decimal newValue)
    {
        Logger.LogDebug($"{this.GetType().Name} rec: {newValue} pwm: {ScaleRangeToPwm(newValue)}");
        _pinInstance.SetPwmValue((float)ScaleRangeToPwm(newValue));
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
    /// Scales a value in the range of -1 to 1 to a normalized PWM value (0-1).
    /// </summary>
    /// <param name="scaledValue">The value to scale, in the range of -1 to 1.</param>
    /// <returns>The corresponding normalized PWM value (0-1).</returns>
    public float ScaleRangeToPwm(decimal scaledValue)
    {
        // -1 -> 0, 0 -> 0.5, 1 -> 1
        return (float)((scaledValue + 1m) / 2m);
    }
}