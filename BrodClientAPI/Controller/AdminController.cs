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
        public IActionResult GetAllUsers()
        {
            var users = _context.User.Find(user => true).ToList(); // Fetch all users from MongoDB
            return Ok(users);
        }

        [HttpPost("create-user")]
        public IActionResult CreateUser([FromBody] User user)
        {
            user._id = "";
            _context.User.InsertOne(user); // Insert a new user into the MongoDB collection
            return Ok(user);
        }

        [HttpGet("tradies")]
        public IActionResult GetAllTradies()
        {
            var tradies = _context.User.Find(user => user.Role == "Tradie").ToList(); // Fetch all tradies from MongoDB
            return Ok(tradies);
        }

        // Fetch tradie details by ID
        [HttpGet("tradie/")]
        public IActionResult GetTradieById([FromBody] OwnProfile getTradieProfile)
        {
            var tradie = _context.User.Find(user => user._id == getTradieProfile.ID && user.Role == "Tradie").FirstOrDefault();
            if (tradie == null)
            {
                return NotFound(new { message = "Tradie not found" });
            }
            return Ok(tradie);
        }

        [HttpPut("tradie/update-status")]
        public IActionResult UpdateTradieStatus([FromBody] UpdateTradieStatus updateTradieStatus)
        {
            var tradie = _context.User.Find(user => user._id == updateTradieStatus.ID && user.Role == "Tradie").FirstOrDefault();
            if (tradie == null)
            {
                return NotFound(new { message = "Tradie not found" });
            }

            // Update the status
            var updateDefinition = Builders<User>.Update.Set(u => u.Status, updateTradieStatus.Status);
            _context.User.UpdateOne(user => user._id == updateTradieStatus.ID, updateDefinition);

            return Ok(new { message = "Tradie status updated successfully" });
        }
    }


}

