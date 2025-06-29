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
        var sessionId = claimsIdentity.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        return await _context.Users.FirstOrDefaultAsync(u => u.SessionToken == sessionId);
    }
}