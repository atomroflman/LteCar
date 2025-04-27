namespace LteCar.Onboard.Hardware;

public class PwmPin : BasePin
{
    public PwmPin(int pinNumber) : base(pinNumber)
    {
        WiringPi.pinMode(pinNumber, WiringPi.PinMode.PWM_OUTPUT);
    }

    public void SetPwmValue(int value)
    {
        WiringPi.pwmWrite(PinNumber, value);
    }
}
