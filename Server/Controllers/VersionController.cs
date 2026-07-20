using LteCar.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace LteCar.Server.Controllers;

[ApiController]
[Route("api/version")]
public class VersionController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    public IServerBuildInfoService BuildInfo { get; }

    public VersionController(IServerBuildInfoService buildInfo)
    {
        BuildInfo = buildInfo;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var info = BuildInfo.GetBuildInfo();
        return Ok(new { branch = info.Branch, commit = info.Commit });
    }
}