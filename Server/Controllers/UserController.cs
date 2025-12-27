using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using LteCar.Server.Data;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace LteCar.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly LteCarContext DbContext;
    private readonly IServiceProvider ServiceProvider;
    private static readonly TimeSpan CodeValidity = TimeSpan.FromMinutes(5);

    public UserController(LteCarContext context, IServiceProvider serviceProvider) : base(context)
    {
        DbContext = context;
        ServiceProvider = serviceProvider;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        string? sessionToken = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            sessionToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        var idEncoder = ServiceProvider.GetRequiredService<Sqids.SqidsEncoder<long>>();
        var userSessionId = string.IsNullOrEmpty(sessionToken)
            ? DbContext.Users.Count() > 0 ? DbContext.Users.Max(u => u.SessionId) + 1 : 1
            : idEncoder.Decode(sessionToken).FirstOrDefault();

        User? user = await DbContext.Users.FirstOrDefaultAsync(u => u.SessionId == userSessionId);

        if (user != null)
        {
            user.LastSeen = DateTime.Now;
            await DbContext.SaveChangesAsync();
            return Ok(new
            {
                authenticated = true,
                userId = user.Id,
                sessionToken
            });
        }

        var newUser = new User
        {
            LastSeen = DateTime.Now,
            SessionId = userSessionId,
        };
        var newSessionToken = idEncoder.Encode(newUser.SessionId);
        DbContext.Users.Add(newUser);
        await DbContext.SaveChangesAsync();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, newSessionToken) };
        var identity = new ClaimsIdentity(claims, "cookie");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("cookie", principal);

        return Ok(new
        {
            authenticated = true,
            userId = newUser.Id,
            sessionToken = newSessionToken
        });
    }


    [HttpPost("generate-transfer-code")]
    public async Task<IActionResult> GenerateTransferCode()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "No active session found" });
        }

        var nextSessionId = await DbContext.GetNextUserSessionId();

        user.TransferCode = nextSessionId;
        user.TransferCodeExpiresAt = DateTime.UtcNow.Add(CodeValidity);
        await DbContext.SaveChangesAsync();
        var idEncoder = ServiceProvider.GetRequiredKeyedService<Sqids.SqidsEncoder<long>>("transfer");
        var transferCode = idEncoder.Encode(user.TransferCode.Value);
        return Ok(new { transferCode });
    }

    [HttpPost("apply-transfer-code")]
    public async Task<IActionResult> ApplyTransferCode([FromBody] ApplyTransferRequest model)
    {
        var code = model.TransferCode?.ToUpper().Trim();
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { message = "Transfer code is required" });
        }

        var transfercodeEncoder = ServiceProvider.GetRequiredKeyedService<Sqids.SqidsEncoder<long>>("transfer");
        var transferCode = transfercodeEncoder.Decode(code).FirstOrDefault();
        if (transferCode == 0)
        {
            return BadRequest(new { message = "Invalid transfer code" });
        }

        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.TransferCode == transferCode && u.TransferCodeExpiresAt > DateTime.UtcNow);
        if (user == null)
        {
            return BadRequest(new { message = "Invalid transfer code" });
        }

        user.TransferCode = null;
        user.TransferCodeExpiresAt = null;

        var idEncoder = ServiceProvider.GetRequiredService<Sqids.SqidsEncoder<long>>();
        var sessionToken = idEncoder.Encode(user.SessionId);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, sessionToken) };
        var identity = new ClaimsIdentity(claims, "cookie");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("cookie", principal);

        await DbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Session transferred successfully"
        });
    }

    public class ApplyTransferRequest
    {
        public string TransferCode { get; set; } = string.Empty;
    }
}
