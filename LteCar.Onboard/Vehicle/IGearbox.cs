namespace LteCar.Onboard.Vehicle;

public interface IGearbox 
{
    public string[] Gears {get;}
    public string CurrentGear {get;}
    public void ShiftUp();
    public void ShiftDown();
}
