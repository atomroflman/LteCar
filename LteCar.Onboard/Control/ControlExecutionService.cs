using System.Text.Json;
using LteCar.Onboard.Control.ControlTypes;

namespace LteCar.Onboard.Control;

public class ControlExecutionService
{
    public IServiceProvider ServiceProvider { get; }
    private readonly Dictionary<string, ControlTypeBase> _controls = new();
    public ControlExecutionService(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
    
    public void Initialize()
    {
        var channelMapFile = new FileInfo("channelMap.json");
        if (!channelMapFile.Exists)
            throw new FileNotFoundException("channelMap.json could not be found");
        var channelMap = JsonSerializer.Deserialize<ChannelMap>(channelMapFile.OpenRead());
        if (channelMap == null)
            throw new Exception("channelMap.json could not be deserialized");
        foreach (var channel in channelMap)
        {
            var controlType = GetControlType(channel.Value.ControlType);
            var control = ServiceProvider.GetService(controlType) as ControlTypeBase;
            if (control == null)
                throw new Exception($"ControlType {channel.Value.ControlType} can not be instantiated");
            _controls.Add(channel.Key, control);
        }
    }
    
    public void SetControl(string channel, decimal value)
    {
        if (!_controls.ContainsKey(channel))
            throw new Exception($"Control {channel} not found");
        var control = _controls[channel];
        control.OnControlRecived(value);
    }

    public void ReleaseControl()
    {
        foreach (var control in _controls)
        {
            control.Value.OnControlReleased();
        }
    }

    private Type GetControlType(string valueControlType)
    {
        var t = typeof(ControlTypeBase).Assembly.GetTypes()
            .FirstOrDefault(type => (type.GetCustomAttributes(typeof(ControlTypeAttribute), false).FirstOrDefault() as ControlTypeAttribute)?.TypeName == valueControlType);
        if (t == null)
            throw new Exception($"ControlType {valueControlType} not found");
        return t;
    }
}

