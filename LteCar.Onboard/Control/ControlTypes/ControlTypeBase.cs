namespace LteCar.Onboard.Control.ControlTypes;

public abstract class ControlTypeBase
{
    public abstract PinFunctionFlags RequiredFunctions { get; }
    public int? Pin { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool TestDisabled {get;set;} = false;
    public Dictionary<string, object> Options {get;set;} = new Dictionary<string, object>();
    public virtual void Initialize() {}
    public async Task RunTestAsync() 
    {
        if (!TestDisabled)
            await RunTestInternalAsync();
    }
    protected virtual Task RunTestInternalAsync() => Task.CompletedTask;
    public abstract void OnControlRecived(decimal newValue);
    public abstract void OnControlReleased();
}