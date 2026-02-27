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
        public async Task<IActionResult> GetCarFunctions(int id)
        {
            // Check if user is authenticated
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not found");

            // Check if user has a setup for this car
            var hasSetup = await _context.UserSetups
                .AnyAsync(u => u.UserId == user.Id && u.CarId == id);
            
            if (!hasSetup)
                return Unauthorized("User has no setup for this car");

            // User has access, return car functions
            var carFunctions = await _context.Set<CarChannel>().Where(c => c.CarId == id).ToListAsync();
            return Ok(carFunctions.Select(cf => new
            {
                id = cf.Id,
                displayName = cf.DisplayName,
                channelName = cf.ChannelName,
                isEnabled = cf.IsEnabled,
                requiresAxis = cf.RequiresAxis,
                maxResendInterval = cf.MaxResendInterval
            }));
        }

        [HttpGet("{carid}/setup")]
        public async Task<IActionResult> GetCarSetup(int carid)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not found");

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.Id == carid);
            if (car == null)
                return NotFound("Car not found");
            var setup = await _context.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == user.Id && u.CarId == carid);

            if (setup == null)
            {
                return NotFound("Setup not found");
            }
            return Ok(new {id =setup.Id, carId = carid, userId = user.Id});
        }

        [HttpGet("{carid}/identity-hash")]
        public async Task<IActionResult> GetCarIdentityHash(int carid)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == carid);
            if (car == null)
                return NotFound("Car not found");

            var hash = LteCar.Shared.HashUtility.GenerateSha256Hash(car.CarIdentityKey);

            return Ok(new { hash });
        }
    }
}