using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LteCar.Server.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace LteCar.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserConfigController : ControllerBase
    {
        private readonly LteCarContext _context;

        public UserConfigController(LteCarContext context)
        {
            _context = context;
        }

        [HttpGet("setup/{carId}")]
        public async Task<IActionResult> GetSetup(int carId)
        {
            // TODO: UserId dynamisch auslesen (z.B. aus Authentifizierung)
            var sessionId = ((ClaimsIdentity)this.User.Identity).Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.SessionToken == sessionId);
            if (user == null)
                return Unauthorized("User not found");

            var setup = await _context.UserSetups
                .Include(u => u.UserSetupChannels)
                .Include(u => u.UserSetupFilters)
                .Include(u => u.UserSetupLinks)
                .FirstOrDefaultAsync(u => u.UserId == user.Id && u.CarId == carId);

            if (setup == null)
            {
                setup = new UserCarSetup
                {
                    CarId = carId,
                    UserId = user.Id,
                    UserSetupChannels = new List<UserSetupChannel>(),
                    UserSetupFilters = new List<UserSetupFilter>(),
                    UserSetupLinks = new List<UserSetupLink>()
                };
            }
            return Ok(setup);
        }

        // Gibt alle verfügbaren Filtertypen zurück
        [HttpGet("filtertypes")]
        public async Task<IActionResult> GetFilterTypes()
        {
            var types = await _context.SetupFilterTypes.ToListAsync();
            return Ok(types);
        }
    }
}
