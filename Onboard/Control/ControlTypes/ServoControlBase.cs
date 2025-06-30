using Microsoft.Extensions.Logging;
using LteCar.Onboard.Hardware;

namespace LteCar.Onboard.Control.ControlTypes;

public abstract class ServoControlBase : ControlTypeBase
{
    IPwmModule _pinInstance;

    public ILogger<ServoControlBase> Logger { get; }

    public ServoControlBase(ILogger<ServoControlBase> logger) : base()
    {
        Logger = logger;
    }

    public override void Initialize()
    {
        _pinInstance = PinManager.GetModule<IPwmModule>(Address ?? 0);
        Logger.LogDebug($"Init: {_pinInstance} Address: {Address}");
        base.Initialize();
    }

    public override void OnControlRecived(decimal newValue)
    {
        Logger.LogDebug($"{this.GetType().Name} rec: {newValue} pwm: {ScaleRangeToPwm((float)newValue)}");
        _pinInstance.SetPwmValue(ScaleRangeToPwm((float)newValue));
    }

    protected override async Task RunTestInternalAsync()
    {
        const int DELAY = 1000;
        for (int i = 0; i < 3; i++)
        {
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
    public float ScaleRangeToPwm(float scaledValue)
    {
        return (scaledValue + 1f) / 2f;
    }
}