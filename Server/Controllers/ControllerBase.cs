using System.Security.Claims;
using LteCar.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace LteCar.Server.Controllers;

public class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
    protected readonly LteCarContext _context;

    public ControllerBase(LteCarContext context)
    {
        _context = context;
    }

    protected async Task<User?> GetCurrentUserAsync()
    {
        if (this.User.Identity is not ClaimsIdentity claimsIdentity)
            return null;
        var sessionString = claimsIdentity.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sessionString))
            return null;
        var idEncoder = HttpContext.RequestServices.GetRequiredService<Sqids.SqidsEncoder<long>>();
        var sessionId = idEncoder.Decode(sessionString).FirstOrDefault();
        return await _context.Users.FirstOrDefaultAsync(u => u.SessionId == sessionId);
    }
}