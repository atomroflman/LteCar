namespace LteCar.Onboard.Hardware;

public class GpioOutputPin : BasePin
{
    internal GpioOutputPin(int pinNumber, IServiceProvider serviceProvider) : base(pinNumber, serviceProvider)
    {
        WiringPi.pinMode(pinNumber, WiringPi.PinMode.OUTPUT);
    }

    public void Write(int value)
    {
        WiringPi.digitalWrite(PinNumber, value);
    }
}
