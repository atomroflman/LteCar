namespace LteCar.Server.Data;

public class UserSetupFunctionNodeParameter : EntityBase
{
    public string ParameterName { get; set; }
    public string? ParameterValue { get; set; }
    public UserSetupFunctionNode Node { get; set; }
    public int NodeId { get; set; }
}