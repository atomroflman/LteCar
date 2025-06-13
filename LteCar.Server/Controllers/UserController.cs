using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using LteCar.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace LteCar.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly LteCarContext DbContext;

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

            // Kein Auth-Cookie oder User nicht gefunden: neuen User anlegen
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
    }
}
