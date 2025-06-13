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
        public CarController(LteCarContext context) : base(context)
        {
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
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not found");

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == carid);
            if (car == null)
                return NotFound("Car not found");
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