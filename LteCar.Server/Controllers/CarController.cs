using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LteCar.Server.Data;
using System.Security.Claims;

namespace LteCar.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarController : ControllerBase
    {
        private readonly LteCarContext _context;

        public CarController(LteCarContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCars()
        {
            var cars = await _context.Cars.ToListAsync();
            return Ok(cars);
        }

        [HttpGet("{id}/functions")]
        public async Task<IActionResult> GetCarFunctions(string id)
        {
            var carFunctions = await _context.Set<CarChannel>().Where(c => c.Car.CarId == id).ToListAsync();
            return Ok(carFunctions.Select(cf => new
            {
                id = cf.Id,
                displayName = cf.DisplayName,
                channelName = cf.ChannelName,
                isEnabled = cf.IsEnabled,
                requiresAxis = cf.RequiresAxis
            }));
        }

        [HttpGet("{carid}/setup")]
        public async Task<IActionResult> GetCarSetup(string carid)
        {
            var sessionId = ((ClaimsIdentity)this.User.Identity).Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.SessionToken == sessionId);
            if (user == null)
                return Unauthorized("User not found");

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == carid);
            var setup = await _context.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == user.Id && u.Car.CarId == carid);

            if (setup == null)
            {
                setup = new UserCarSetup
                {
                    CarId = car.Id,
                    UserId = user.Id
                };
                _context.UserSetups.Add(setup);
                await _context.SaveChangesAsync();
            }
            return Ok(new {id =setup.Id, carId = carid, userId = user.Id});
        }
    }
}