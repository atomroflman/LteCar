using LteCar.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace LteCar.Server.Controllers;

[ApiController]
[Route("api/install")]
public class InstallController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    public IOnboardInstallScriptService OnboardInstallScriptService { get; }

    public InstallController(IOnboardInstallScriptService onboardInstallScriptService)
    {
        OnboardInstallScriptService = onboardInstallScriptService;
    }

    [HttpGet("onboard-command")]
    public IActionResult GetOnboardCommand([FromQuery] string? branch = null)
    {
        var installCommand = OnboardInstallScriptService.BuildOnboardInstallCommand(Request, branch);
        return Ok(new
        {
            installCommand.Command,
            installCommand.ScriptUrl,
            installCommand.ServerName,
            installCommand.ServerPort,
            installCommand.UseHttps,
            installCommand.ServerUrl,
            installCommand.Branch,
            installCommand.GitRef
        });
    }

    [HttpGet("onboard.sh")]
    public IActionResult GetOnboardScript([FromQuery] string? branch = null)
    {
        var script = OnboardInstallScriptService.BuildOnboardInstallScript(Request, branch);
        Response.Headers.ContentDisposition = "inline; filename=ltecar-onboard-install.sh";
        return Content(script, "text/x-shellscript; charset=utf-8");
    }
}
