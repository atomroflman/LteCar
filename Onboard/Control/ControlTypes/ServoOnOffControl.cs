using Microsoft.Extensions.Logging;
using LteCar.Onboard.Hardware;

namespace LteCar.Onboard.Control.ControlTypes;

[ControlType("ServoOnOff")]
public class ServoOnOffControl : ServoControlBase
{
    private bool _isOn = false;
    private float _onPosition = 1.0f;    // Position für "An" (Standard: 1.0)
    private float _offPosition = -1.0f;  // Position für "Aus" (Standard: -1.0)

    public ServoOnOffControl(ILogger<ServoOnOffControl> logger) : base(logger)
    {
    }

    public override void Initialize()
    {
        base.Initialize();
        
        // Konfiguriere Positionen aus Options falls vorhanden
        if (Options.ContainsKey("onPosition") && float.TryParse(Options["onPosition"].ToString(), out float onPos))
        {
            _onPosition = onPos;
        }
        
        if (Options.ContainsKey("offPosition") && float.TryParse(Options["offPosition"].ToString(), out float offPos))
        {
            _offPosition = offPos;
        }

        Logger.LogDebug($"ServoOnOffControl initialized - OnPosition: {_onPosition}, OffPosition: {_offPosition}");
        
        // Starte in OFF Position
        SetServoOff();
    }

    public override void OnControlRecived(decimal newValue)
    {
        // Bei positivem Wert: An, bei negativem oder null: Aus
        bool shouldBeOn = newValue > 0;
        
        if (shouldBeOn != _isOn)
        {
            if (shouldBeOn)
            {
                SetServoOn();
            }
            else
            {
                SetServoOff();
            }
        }
    }

    public override void OnControlReleased()
    {
        // Bei Release immer ausschalten
        SetServoOff();
    }

    private void SetServoOn()
    {
        _isOn = true;
        Logger.LogDebug($"Servo ON - Position: {_onPosition}");
        base.OnControlRecived((decimal)_onPosition);
    }

    private void SetServoOff()
    {
        _isOn = false;
        Logger.LogDebug($"Servo OFF - Position: {_offPosition}");
        base.OnControlRecived((decimal)_offPosition);
    }

    protected override async Task RunTestInternalAsync()
    {
        const int DELAY = 2000; // 2 Sekunden für bessere Sichtbarkeit
        
        Logger.LogInformation("Starting ServoOnOff test...");
        
        for (int i = 0; i < 3; i++)
        {
            Logger.LogInformation($"Test cycle {i + 1}/3 - Turning ON");
            SetServoOn();
            await Task.Delay(DELAY);
            
            Logger.LogInformation($"Test cycle {i + 1}/3 - Turning OFF");
            SetServoOff();
            await Task.Delay(DELAY);
        }
        
        Logger.LogInformation("ServoOnOff test completed");
        OnControlReleased();
    }

    // Hilfsmethoden für externe Nutzung
    public void TurnOn()
    {
        OnControlRecived(1);
    }

    public void TurnOff()
    {
        OnControlRecived(0);
    }

    public void Toggle()
    {
        if (_isOn)
            TurnOff();
        else
            TurnOn();
    }

    public bool IsOn => _isOn;
}