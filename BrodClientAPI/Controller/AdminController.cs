using BrodClientAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BrodClientAPI.Data;
using MongoDB.Driver;
using SendGrid.Helpers.Mail;

namespace BrodClientAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class AdminController : ControllerBase 
    {
        private readonly ApiDbContext _context;
        private readonly IConfiguration _configuration;
        public AdminController(ApiDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration; 
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
        [HttpPost("GetFilteredTradies")]
        public async Task<IActionResult> GetFilteredTradies([FromBody] UserFilter filterInput)
        {
            try
            {
                // Fetch active services
                var tradies = await _context.User.Find(user => user.Role == "Tradie").ToListAsync();

                // Apply additional filters
                if (!string.IsNullOrEmpty(filterInput.TypeOfWork))
                {
                    tradies = tradies.Where(s => s.TypeofWork == filterInput.TypeOfWork).ToList();
                }
                if (!string.IsNullOrEmpty(filterInput.Status))
                {
                    tradies = tradies.Where(s => s.Status == filterInput.Status).ToList();
                }
                if (filterInput.SubmissionDateFrom != null )
                {
                    tradies = tradies.Where(s => s.TimeStamp >= filterInput.SubmissionDateFrom ).ToList();
                }
                if (filterInput.SubmissionDateTo != null)
                {
                    tradies = tradies.Where(s => s.TimeStamp <= filterInput.SubmissionDateTo).ToList();
                }
                if (!string.IsNullOrEmpty(filterInput.Keyword))
                {
                    var keyword = filterInput.Keyword.ToLower();
                    tradies = tradies.Where(s =>
                        s.FirstName.ToLower().Contains(keyword) ||
                        s.LastName.ToLower().Contains(keyword)
                        ).ToList();
                }

                return Ok(tradies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving tradies", error = ex.Message });
            }
        }

        [HttpPost("GetFilteredUsers")]
        public async Task<IActionResult> GetFilteredUsers([FromBody] UserFilter filterInput)
        {
            try
            {
                // Fetch active services
                var users = await _context.User.Find(user => true).ToListAsync();

                // Apply additional filters
                if (!string.IsNullOrEmpty(filterInput.TypeOfWork))
                {
                    users = users.Where(s => s.TypeofWork == filterInput.TypeOfWork).ToList();
                }
                if (!string.IsNullOrEmpty(filterInput.Status))
                {
                    users = users.Where(s => s.Status == filterInput.Status).ToList();
                }
                if (filterInput.SubmissionDateFrom != null)
                {
                    users = users.Where(s => s.TimeStamp >= filterInput.SubmissionDateFrom).ToList();
                }
                if (filterInput.SubmissionDateTo != null)
                {
                    users = users.Where(s => s.TimeStamp <= filterInput.SubmissionDateTo).ToList();
                }
                if (!string.IsNullOrEmpty(filterInput.Keyword))
                {
                    var keyword = filterInput.Keyword.ToLower();
                    users = users.Where(s =>
                        s.FirstName.ToLower().Contains(keyword) ||
                        s.LastName.ToLower().Contains(keyword)).ToList();
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving users", error = ex.Message });
            }
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
            var tradie = await _context.User.Find(user => user._id == updateTradieStatus.ID).FirstOrDefaultAsync();
            if (tradie == null)
            {
                return NotFound();
            }

            // Update the status
            var updateDefinition = Builders<User>.Update.Set(u => u.Status, updateTradieStatus.Status);
            await _context.User.UpdateOneAsync(user => user._id == updateTradieStatus.ID, updateDefinition);

            // SendGrid API Key from configuration
            var sendGridApiKey = _configuration["SendGrid:ApiKey"];
            var fromEmailAddress = _configuration["SendGrid:FromEmailAddress"];

            // Initialize SendGrid client
            var client = new SendGrid.SendGridClient(sendGridApiKey);
            var subject = "";
            var plainTextContent = "";
            var htmlContent = "";
            if (updateTradieStatus.Status.ToLower() == "approved") {
                subject = "Your Account Has Been Approved!";
                plainTextContent = $@"
                    Hi {tradie.FirstName},

                    Congratulations! We're happy to inform you that your account with BROD has been approved. You can now start creating and managing your job ads.

                    If you need any help getting started, feel free to reach out to our support team at support@brod.com.au.

                    Welcome aboard, and we look forward to helping you succeed!

                    Best regards,
                    BROD Team";
                htmlContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif; color: #000000;'>
                            <p>Hi {tradie.FirstName},</p>
                            <br><p>Congratulations! We're happy to inform you that your account with BROD has been approved. You can now start creating and managing your job ads.</p>
                            <br><p>If you need any help getting started, feel free to reach out to our support team at support@brod.com.au.</p>
                            <br><p>Welcome aboard, and we look forward to helping you succeed!</p>
                            <br><p>Best regards,</p>
                            <p><strong>BROD Team</strong></p>
                        </body>
                    </html>";
            }
            if (updateTradieStatus.Status.ToLower() == "declined")
            {
                subject = "Your Account Application Has Been Declined";
                plainTextContent = $@"
                    Hi {tradie.FirstName},

                    Thank you for applying to BROD. Unfortunately, we were unable to approve your account at this time.

                    If you'd like to know more about why your application was declined or if you need assistance with the next steps, please feel free to contact our support team at support@brod.com.au. We're happy to help you verify any missing information or address any concerns.

                    We appreciate your understanding and look forward to resolving this with you.

                    Best regards,
                    BROD Team";
                htmlContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif; color: #000000;'>
                            <p>Hi {tradie.FirstName},</p>
                            <br><p>Thank you for applying to BROD. Unfortunately, we were unable to approve your account at this time.</p>
                            <br><p>If you'd like to know more about why your application was declined or if you need assistance with the next steps, please feel free to contact our support team at support@brod.com.au. We're happy to help you verify any missing information or address any concerns.</p>
                            <br><p>We appreciate your understanding and look forward to resolving this with you.</p>
                            <br><p>Best regards,</p>
                            <p><strong>BROD Team</strong></p>
                        </body>
                    </html>";
            }

            // Prepare email message
            var from = new SendGrid.Helpers.Mail.EmailAddress(fromEmailAddress, "Brod System");
            var to = new SendGrid.Helpers.Mail.EmailAddress(tradie.Email);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

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
            if (user.weeksSuspended != suspendUser.weeksSuspended)
            {
                updateDefinitions.Add(Builders<User>.Update.Set(u => u.weeksSuspended, suspendUser.weeksSuspended));
            }
            if (user.suspensionStartDate != suspendUser.suspensionStartDate)
            {
                updateDefinitions.Add(Builders<User>.Update.Set(u => u.suspensionStartDate, suspendUser.suspensionStartDate));
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

