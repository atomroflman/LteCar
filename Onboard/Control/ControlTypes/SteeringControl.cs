using LteCar.Onboard.Hardware;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Control.ControlTypes;

[ControlType("Steering")]
public class SteeringControl : ServoControlBase 
{ 
    public SteeringControl(ILogger<SteeringControl> logger) : base(logger)
    {        
    }

    public override string ToString() => $"Steering@{Address}";
}
