namespace LteCar.Onboard.Hardware;

public class GpioOutputPin : BasePin
{
    internal GpioOutputPin(int pinNumber) : base(pinNumber)
    {
        WiringPi.pinMode(pinNumber, WiringPi.PinMode.OUTPUT);
    }

    public void Write(int value)
    {
        WiringPi.digitalWrite(PinNumber, value);
    }
}
