using LteCar.Onboard.Vehicle;

namespace LteCar.Onboard.Control.ControlTypes;

public class GearControl : ControlTypeBase 
{
    private IGearbox _gearbox;
    private bool _shiftUp = false;
    public GearControl(IGearbox gearbox)
    {
        _gearbox = gearbox;
    }

    public override PinFunctionFlags RequiredFunctions => PinFunctionFlags.None;

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
        }
    }

    public override void OnControlReleased()
    {
    }
}