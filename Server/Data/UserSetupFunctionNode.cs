namespace LteCar.Server.Data;

public class UserSetupFunctionNode : UserSetupFlowNodeBase
{
    public string SetupFunctionName { get; set; }
    public ICollection<UserSetupFunctionNodeParameter> Parameters { get; set; } = new List<UserSetupFunctionNodeParameter>();
}
