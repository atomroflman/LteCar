using System.Reflection;
using System.Text.Json;
using LteCar.Onboard.Control.ControlTypes;
using LteCar.Onboard.Hardware;
using LteCar.Shared.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Control;

public class ControlExecutionService
{
    public IServiceProvider ServiceProvider { get; }
    public ILogger<ControlExecutionService> Logger { get; }

    private readonly Dictionary<string, ControlTypeBase> _controls = new();
    private readonly ChannelMap _channelMap;

    public ControlExecutionService(ChannelMap channelMap, IServiceProvider serviceProvider, ILogger<ControlExecutionService> logger)
    {
        _channelMap = channelMap;
        ServiceProvider = serviceProvider;
        Logger = logger;
    }
    
    public void Initialize()
    {
        foreach (var channel in _channelMap.ControlChannels)
        {
            var controlType = GetControlType(channel.Value.ControlType);
            Logger.LogDebug($"Got type {controlType.Name} for channel {channel.Key}.");
            var control = ServiceProvider.GetService(controlType) as ControlTypeBase;
            if (control == null)
                throw new Exception($"ControlType '{channel.Value.ControlType}' can not be instantiated");
            
            var pinManagerName = channel.Value.PinManager;
            if (string.IsNullOrWhiteSpace(pinManagerName))
                pinManagerName = "default"; // Default pin manager if not specified
            var pinManager = ServiceProvider.GetRequiredService<IModuleManagerFactory>().Create(pinManagerName);
            if (pinManager == null)
                throw new Exception($"Pin manager '{pinManagerName}' not found in ChannelMap.");
            control.PinManager = pinManager;
            control.Name = channel.Key;
            control.Options = channel.Value.Options;
            control.TestDisabled = channel.Value.TestDisabled;
            control.Initialize();
            _controls.Add(channel.Key, control);
            Logger.LogInformation($"Initialized Channel: {channel.Key} - {channel.Value.ControlType}@{pinManagerName}:{channel.Value.Address}");
        }
    }

    public async Task RunControlTestsAsync() 
    {
        foreach (var c in _controls) {
            Logger.LogInformation($"Testing: {c.Key}");
            await c.Value.RunTestAsync();
        }
    }
    
    public void SetControl(string channel, decimal value)
    {
        if (!_controls.ContainsKey(channel))
        {
            Logger.LogError($"Channel not configured: {channel}");
            return;
        }
        var control = _controls[channel];
        Logger.LogDebug($"New Channel Value: {channel}: {value} => PIN: {control}");
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

