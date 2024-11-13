using BrodClientAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BrodClientAPI.Data;
using MongoDB.Driver;

namespace BrodClientAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class AdminController : ControllerBase 
    {
        private readonly ApiDbContext _context;

        public AdminController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.User.Find(user => true).ToListAsync(); // Fetch all users from MongoDB
            return Ok(users);
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            user._id = "";
            await _context.User.InsertOneAsync(user); // Insert a new user into the MongoDB collection
            return Ok(user);
        }

        [HttpGet("tradies")]
        public async Task<IActionResult> GetAllTradies()
        {
            var tradies = await _context.User.Find(user => user.Role == "Tradie").ToListAsync(); // Fetch all tradies from MongoDB
            return Ok(tradies);
        }

        // Fetch tradie details by ID
        [HttpPost("tradieProfileByID")]
        public async Task<IActionResult> GetTradieById([FromBody] OwnProfile getTradieProfile)
        {
            var tradie = await _context.User.Find(user => user._id == getTradieProfile.ID).FirstOrDefaultAsync();
            if (tradie == null)
            {
                return NotFound();
            }

            var rating = await _context.Rating.Find(rating => rating.tradieId == getTradieProfile.ID).ToListAsync();
            if (rating == null)
            {
                return NotFound();
            }

            var model = new TradieProfile
            {
                user = tradie,
                ratings = rating
            };
            return Ok(model);
        }

        [HttpPut("tradie/update-status")]
        public async Task<IActionResult> UpdateTradieStatus([FromBody] UpdateTradieStatus updateTradieStatus)
        {
            var tradie = await _context.User.Find(user => user._id == updateTradieStatus.ID && user.Role == "Tradie").FirstOrDefaultAsync();
            if (tradie == null)
            {
                return NotFound();
            }

            // Update the status
            var updateDefinition = Builders<User>.Update.Set(u => u.Status, updateTradieStatus.Status);
            await _context.User.UpdateOneAsync(user => user._id == updateTradieStatus.ID, updateDefinition);

            return Ok(new { message = "Tradie status updated successfully" });
        }

        [HttpPut("suspendUser")]
        public async Task<IActionResult> SuspendUser([FromBody] SuspendUser suspendUser)
        {
            var user = await _context.User.Find(user => user._id == suspendUser.userID).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound();
            }

            var updateDefinitions = new List<UpdateDefinition<User>>();
            if (user.isSuspended != suspendUser.isSuspended)
            {
                updateDefinitions.Add(Builders<User>.Update.Set(u => u.isSuspended, suspendUser.isSuspended));
            }
            if (user.weeksSuspended != suspendUser.WeeksSuspended)
            {
                updateDefinitions.Add(Builders<User>.Update.Set(u => u.weeksSuspended, suspendUser.WeeksSuspended));
            }

            if (updateDefinitions.Count == 0)
            {
                return BadRequest(new { message = "No valid fields to update" });
            }

            var updateDefinition = Builders<User>.Update.Combine(updateDefinitions);
            var filter = Builders<User>.Filter.Eq(u => u._id, suspendUser.userID);

            await _context.User.UpdateOneAsync(filter, updateDefinition);

            return Ok(new { message = "User suspended successfully" });
        }

    }


}

