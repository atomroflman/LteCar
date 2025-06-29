using LteCar.Onboard.Hardware;

public interface IModuleManagerFactory
{
    IModuleManager Create(string name);
}

