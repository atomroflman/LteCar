namespace LteCar.Onboard.Hardware;

public class GpioInputPin : BasePin
{
    internal GpioInputPin(int pinNumber) : base(pinNumber)
    {
        WiringPi.pinMode(pinNumber, WiringPi.PinMode.INPUT);
    }

    public int Read()
    {
        return WiringPi.digitalRead(PinNumber);
    }
}
