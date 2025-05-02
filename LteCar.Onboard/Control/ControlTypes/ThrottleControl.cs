using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Control.ControlTypes;

[ControlType("Throttle")]
public class ThrottleControl : ServoControlBase 
{
    public ThrottleControl(ILogger<ThrottleControl> logger) : base(logger)
    {        
    }
 }