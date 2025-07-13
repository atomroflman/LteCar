using LteCar.Onboard.Hardware;
using LteCar.Onboard.Vehicle;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Control.ControlTypes;

[ControlType("Throttle")]
public class ThrottleControl : ServoControlBase 
{
    public IGearbox Gearbox { get; }

    public ThrottleControl(ILogger<ThrottleControl> logger, IGearbox gearbox) : base(logger)
    {
        Gearbox = gearbox;
    }

    public override string ToString() => $"Throttle@{Address}";

    public override void OnControlRecived(decimal newValue)
    {
        switch (Gearbox.CurrentGear)         {
            case "D":
                base.OnControlRecived(Math.Max(newValue, 0));
                return;
            case "N":
                base.OnControlRecived(0);
                return;
            case "R":
                base.OnControlRecived(-Math.Max(newValue, 0));
                return;
        }
    }
 }