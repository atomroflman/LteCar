namespace LteCar.Shared;

/// <summary>
/// Interface for the Browser client to recieve new car address if changed.
/// (hopefully fast enough)
/// </summary>
public interface IConnectionHubClient
{
    Task UpdateCarAddress(string address);
}