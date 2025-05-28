using LteCar.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LteCar.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlowController : ControllerBase
    {
        private readonly LteCarContext _context;

        public FlowController(LteCarContext context)
        {
            _context = context;
        }

        [HttpGet("{userSetupId}")]
        public async Task<IActionResult> GetFlow(int userSetupId)
        {
            var flowNodes = await _context.Set<UserSetupFlowNodeBase>()
                .Where(n => n.UserSetupId == userSetupId)
                .ToListAsync();
            if (flowNodes == null || !flowNodes.Any())
            {
                return Ok(new
                {
                    nodes = new List<object>(),
                    links = new List<object>()
                });
            }

            var loadedNodes = flowNodes.Select(n => n.Id).ToArray();

            return Ok(new
            {
                nodes = flowNodes.Select(n => new
                {
                    id = n.Id,
                    type = n.GetType().Name,
                    positionX = n.PositionX,
                    positionY = n.PositionY
                }),
                links = await _context.Set<UserSetupLink>()
                    .Where(l => loadedNodes.Contains(l.UserSetupFromNodeId) && loadedNodes.Contains(l.UserSetupToNodeId))
                    .Select(l => new
                    {
                        source = l.UserSetupFromNodeId,
                        target = l.UserSetupToNodeId,
                    })
                    .ToListAsync()
            }); 
        }

        [HttpPost("addnode")]
        public async Task<IActionResult> AddNode([FromBody] AddNodeRequest req)
        {
            UserSetupFlowNodeBase node;
            switch (req.Type?.ToLowerInvariant())
            {
                case "userchannel":
                    // if (string.IsNullOrEmpty(req.ChannelName))
                    //     return BadRequest("Channel name is required for UserChannel node");
                    // var channel = await _context.Set<UserChannel>()
                    //     .FirstOrDefaultAsync(c => c.ChannelName == req.ChannelName);
                    // if (channel == null)
                    //     return NotFound("UserChannel not found");
                    
                    // node = new UserSetupUserChannelNode
                    // {
                    //     UserSetupId = req.UserSetupId,
                    //     UserChannelId = channel.Id,
                    //     PositionX = req.PositionX,
                    //     PositionY = req.PositionY
                    // };
                    break;
                case "carchannel":
                    // node = new UserSetupCarChannelNode { UserSetupId = req.UserSetupId, CarChannelId = req.ElementId, PositionX = req.PositionX, PositionY = req.PositionY };
                    break;
                case "function":
                    // node = new UserSetupFunctionNode { UserSetupId = req.UserSetupId, SetupFunctionId = req.ElementId, PositionX = req.PositionX, PositionY = req.PositionY };
                    break;
                default:
                    return BadRequest("Unknown node type");
            }
            // _context.Add(node);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                // id = node.Id
            });
        }

        public class AddNodeRequest
        {
            public int UserSetupId { get; set; }
            public string Type { get; set; } = string.Empty;
            public string ChannelName { get; set; }
            public float PositionX { get; set; } = 100;
            public float PositionY { get; set; } = 100;
        }

        [HttpPost("movenode")]
        public async Task<IActionResult> MoveNode([FromBody] MoveNodeRequest req)
        {
            var node = await _context.Set<UserSetupFlowNodeBase>().FirstOrDefaultAsync(n => n.Id == req.NodeId);
            if (node == null) return NotFound();
            node.PositionX = req.PositionX;
            node.PositionY = req.PositionY;
            await _context.SaveChangesAsync();
            return Ok();
        }

        public class MoveNodeRequest
        {
            public int NodeId { get; set; }
            public float PositionX { get; set; }
            public float PositionY { get; set; }
        }

        [HttpPost("link")]
        public async Task<IActionResult> LinkNode([FromBody] LinkNodeRequest req)
        {
            var from = await _context.Set<UserSetupFlowNodeBase>().FirstOrDefaultAsync(n => n.Id == req.FromNodeId);
            var to = await _context.Set<UserSetupFlowNodeBase>().FirstOrDefaultAsync(n => n.Id == req.ToNodeId);
            if (from == null || to == null)
                return NotFound("Node(s) not found");

            if (to is UserSetupUserChannelNode)
                return BadRequest("Cannot link to UserChannelNode");
            
            if (from is UserSetupCarChannelNode)
                return BadRequest("Cannot link from UserChannelNode");
            
            // only 1 incoming link allowed
            var otherToLink = await _context.Set<UserSetupLink>()
                .FirstOrDefaultAsync(l => l.UserSetupToNodeId == req.ToNodeId);
            if (otherToLink != null)
            {
                if (otherToLink.UserSetupFromNodeId == req.FromNodeId)
                    return Ok(new { id = otherToLink.Id }); // already linked
                _context.Remove(otherToLink); // remove existing link
            }

            var link = new UserSetupLink
            {
                UserSetupFromNodeId = req.FromNodeId,
                UserSetupToNodeId = req.ToNodeId
            };
            _context.Add(link);
            await _context.SaveChangesAsync();
            return Ok(new { id = link.Id });
        }

        public class LinkNodeRequest
        {
            public int FromNodeId { get; set; }
            public int ToNodeId { get; set; }
        }

        [HttpPost("unlink")]
        public async Task<IActionResult> UnlinkNode([FromBody] LinkNodeRequest req)
        {
            var link = await _context.Set<UserSetupLink>()
                .FirstOrDefaultAsync(l => l.UserSetupFromNodeId == req.FromNodeId && l.UserSetupToNodeId == req.ToNodeId);
            if (link == null) return NotFound();
            _context.Remove(link);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
