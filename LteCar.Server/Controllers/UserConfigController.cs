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

        public UserConfigController(LteCarContext context) : base(context)
        {
            _context = context;
        }

        [HttpGet("setup/{carId}")]
        public async Task<IActionResult> GetSetup(string carId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not found");

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == carId);
            if (car == null)
                return NotFound("Car not found");
            var setup = await _context.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == user.Id && u.Car.CarId == carId);

            if (setup == null)
            {
                setup = new UserCarSetup
                {
                    CarId = car.Id,
                    UserId = user.Id
                };
                _context.UserSetups.Add(setup);
                _context.SaveChanges();
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

        // Gibt alle Gamepads des Users zurück
        [HttpGet("gamepads")]
        public async Task<IActionResult> GetUserGamepads()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not found");
            var gamepads = _context.Set<UserChannelDevice>()
                .Where(g => g.UserId == user.Id);
            return Ok(await gamepads.Select(e => new
            {
                id = e.Id,
                name = e.DeviceName,
                axes = e.Channels.Where(c => c.IsAxis).Select(c => new
                {
                    id = c.Id,
                    channelId = c.ChannelId,
                    name = c.Name,
                    calibrationMin = c.CalibrationMin,
                    calibrationMax = c.CalibrationMax,
                    accuracy = c.Accuracy
                }),
                buttons = e.Channels.Where(c => !c.IsAxis).Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    channelId = c.ChannelId
                })
            }).ToListAsync());
        }

        private static object _registerLock = new object();

        [HttpPost("register-gamepad")]
        public IActionResult RegisterUserGamepad([FromBody] RegisterGamepadRequest req)
        {
            lock (_registerLock)
            {
                var sessionId = ((ClaimsIdentity)this.User.Identity).Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var user = _context.Users.FirstOrDefault(u => u.SessionToken == sessionId);
                if (user == null)
                    return Unauthorized("User not found");
                // Prüfe, ob Gamepad schon existiert
                var exists = _context.Set<UserChannelDevice>()
                    .FirstOrDefault(g => g.UserId == user.Id && g.DeviceName == req.DeviceName);
                if (exists is not null)
                    return Ok(new
                    {
                        id = exists.Id,
                        name = exists.DeviceName,
                        axes = _context.Set<UserChannel>()
                            .Where(c => c.UserChannelDeviceId == exists.Id && c.IsAxis)
                            .Select(c => new
                            {
                                id = c.Id,
                                channelId = c.ChannelId,
                                name = c.Name,
                                calibrationMin = c.CalibrationMin,
                                calibrationMax = c.CalibrationMax,
                                accuracy = c.Accuracy
                            }),
                        buttons = _context.Set<UserChannel>()
                            .Where(c => c.UserChannelDeviceId == exists.Id && !c.IsAxis)
                            .Select(c => new
                            {
                                id = c.Id,
                                name = c.Name,
                                channelId = c.ChannelId
                            })
                    });
                var gamepad = new UserChannelDevice
                {
                    UserId = user.Id,
                    DeviceName = req.DeviceName,
                };
                _context.Set<UserChannelDevice>().Add(gamepad);
                for (int i = 0; i < req.Axes; i++)
                {
                    _context.Set<UserChannel>().Add(new UserChannel
                    {
                        UserChannelDevice = gamepad,
                        CalibrationMax = 1,
                        CalibrationMin = -1,
                        IsAxis = true,
                        ChannelId = i,
                        Name = $"Axis {i + 1}"
                    });
                }
                for (int i = 0; i < req.Buttons; i++)
                {
                    _context.Set<UserChannel>().Add(new UserChannel
                    {
                        UserChannelDevice = gamepad,
                        IsAxis = false,
                        ChannelId = i,
                        Name = $"Button {i + 1}"
                    });
                }
                _context.SaveChanges();
                return Ok(new
                {
                    id = gamepad.Id,
                    name = gamepad.DeviceName,
                    axes = _context.Set<UserChannel>()
                        .Where(c => c.UserChannelDeviceId == gamepad.Id && c.IsAxis)
                        .Select(c => new
                        {
                            id = c.Id,
                            name = c.Name,
                            calibrationMin = c.CalibrationMin,
                            calibrationMax = c.CalibrationMax
                        }),
                    buttons = _context.Set<UserChannel>()
                        .Where(c => c.UserChannelDeviceId == gamepad.Id && !c.IsAxis)
                        .Select(c => new
                        {
                            id = c.Id,
                            name = c.Name
                        })
                });
            }
        }

        [HttpPost("gamepad-axis-accuracy")]
        public async Task<IActionResult> SetGamepadAxisAccuracy([FromBody] SetGamepadAxisAccuracyRequest req)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not found");
            var channel = _context.Set<UserChannel>()
                .FirstOrDefault(c => c.ChannelId == req.ChannelIndex
                    && c.UserChannelDevice.UserId == user.Id
                    && c.IsAxis
                    && c.UserChannelDevice.DeviceName == req.GamepadId);
            if (channel == null)
                return NotFound("Channel not found");
            channel.Accuracy = req.Accuracy;
            _context.SaveChanges();
            return Ok();
        }


        public class RegisterGamepadRequest
        {
            public string DeviceName { get; set; }
            public int Axes { get; set; }
            public int Buttons { get; set; }
        }
    }

    public class SetGamepadAxisAccuracyRequest
    {
        public string GamepadId { get; set; }
        public int ChannelIndex { get; set; }
        public int Accuracy { get; set; }
    }
}
