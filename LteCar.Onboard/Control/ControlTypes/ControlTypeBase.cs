using LteCar.Onboard.Hardware;

namespace LteCar.Onboard.Control.ControlTypes;

public abstract class ControlTypeBase
{
    public int? Address { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool TestDisabled { get; set; } = false;
    public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
    public IModuleManager PinManager { get; set; }

    public virtual void Initialize() { }
    public async Task RunTestAsync()
    {
        if (!TestDisabled)
            await RunTestInternalAsync();
    }
    protected virtual Task RunTestInternalAsync() => Task.CompletedTask;
    public abstract void OnControlRecived(decimal newValue);
    public abstract void OnControlReleased();
}