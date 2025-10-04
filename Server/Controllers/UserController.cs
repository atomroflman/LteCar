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
        private static readonly TimeSpan CodeValidity = TimeSpan.FromMinutes(5);

        public UserController(LteCarContext context) : base(context)
        {
            DbContext = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            string? sessionToken = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                sessionToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            User? user = null;
            if (!string.IsNullOrEmpty(sessionToken))
            {
                user = await DbContext.Users.FirstOrDefaultAsync(u => u.SessionToken == sessionToken);
            }

            if (user != null)
            {
                user.LastSeen = DateTime.Now;
                await DbContext.SaveChangesAsync();
                return Ok(new { authenticated = true, userId = user.Id, sessionToken = user.SessionToken });
            }

            var newSessionToken = Guid.NewGuid().ToString();
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, newSessionToken) };
            var identity = new ClaimsIdentity(claims, "cookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("cookie", principal);

            var newUser = new User
            {
                SessionToken = newSessionToken,
                LastSeen = DateTime.Now
            };
            DbContext.Users.Add(newUser);
            await DbContext.SaveChangesAsync();

            return Ok(new { authenticated = true, userId = newUser.Id, sessionToken = newUser.SessionToken });
        }
    

    [HttpPost("generate-transfer-code")]
    public async Task<IActionResult> GenerateTransferCode()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new { message = "No active session found" });
        }
        
        string transferCode;
        do
        {
            transferCode = GenerateRandomCode(6);
        } 
        while (await DbContext.Users.AnyAsync(u => u.TransferCode == transferCode));
        
        user.TransferCode = transferCode;
        user.TransferCodeExpiresAt = DateTime.UtcNow.Add(CodeValidity);
        await DbContext.SaveChangesAsync();
        
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
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.TransferCode == code && u.TransferCodeExpiresAt > DateTime.UtcNow);
        if (user == null)
        {
            return BadRequest(new { message = "Invalid transfer code" });
        }

        user.TransferCode = null;
        user.TransferCodeExpiresAt = null;
        
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.SessionToken) };
        var identity = new ClaimsIdentity(claims, "cookie");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("cookie", principal);

        await DbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Session transferred successfully"
        });
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        random.GetBytes(bytes);
        
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    public class ApplyTransferRequest
    {
        public string TransferCode { get; set; } = string.Empty;
    }
}
