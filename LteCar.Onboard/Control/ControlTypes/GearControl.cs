using LteCar.Onboard.Vehicle;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Control.ControlTypes;

[ControlType("GearShift")]
public class GearControl : ControlTypeBase 
{
    private IGearbox _gearbox;
    private bool _shiftUp = false;
    public GearControl(IGearbox gearbox, ILogger<GearControl> logger)
    {
        _gearbox = gearbox;
        Logger = logger;
    }

    public override PinFunctionFlags RequiredFunctions => PinFunctionFlags.None;

    public ILogger<GearControl> Logger { get; }

    public override void Initialize()
    {
        base.Initialize();
        if (Options.TryGetValue("shiftDirection", out object value)) {
            _shiftUp = value.ToString().ToLower() == "up";
        }
    }

    public override void OnControlRecived(decimal newValue)
    {
        if (newValue == 1) {
            if (_shiftUp)
                _gearbox.ShiftUp();
            else 
                _gearbox.ShiftDown();
            Logger.LogDebug($"Switched gear to: {_gearbox.CurrentGear}");
        }
    }

    public override void OnControlReleased()
    {
    }
}