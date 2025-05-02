public class CarConnectionHub : Hub<IConnectionHubClient>
{
    public async Task UpdateCarAddress(string carId)
    {
        var remoteIp = Context.GetHttpContext().Connection.RemoteIpAddress;
        await Clients.Groups(carId).UpdateCarAddress(remoteIp.ToString());
    }
}