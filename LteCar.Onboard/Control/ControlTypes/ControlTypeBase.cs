namespace LteCar.Onboard.Control.ControlTypes;

public abstract class ControlTypeBase
{
    public abstract PinFunctionFlags RequiredFunctions { get; }
    public int Pin { get; set; }
    public string Name { get; set; } = string.Empty;
    public virtual void Initialize() {}
    public abstract void OnControlRecived(decimal newValue);
    public abstract void OnControlReleased();
}