namespace LteCar.Onboard.Vehicle;

public class VirtualAutomaticGearbox : IGearbox
{
    static string[] _gears = new string[] {"R", "N", "D"};
    private byte _gearIndex = 2;
    public string[] Gears => _gears;
    public string CurrentGear => _gears[_gearIndex];

    public void ShiftDown()
    {
        if (_gearIndex > 0)
            _gearIndex--;
    }

    public void ShiftUp()
    {
        if (_gearIndex < _gears.Length - 1)
            _gearIndex++;
    }
}