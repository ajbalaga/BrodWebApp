using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using BrodClientAPI.Data;
using BrodClientAPI.Models;
using MongoDB.Driver;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using SendGrid;
using SendGrid.Helpers.Mail;
using MongoDB.Bson;
using Google.Apis.Auth;

namespace BrodClientAPI.Controller
{
        [ApiController]
        [Route("api/[controller]")]
        public class AuthController : ControllerBase
        {
            private readonly ApiDbContext _context;
            private readonly IConfiguration _configuration;

            public AuthController(ApiDbContext context, IConfiguration configuration)
                {
                _context = context;
                _configuration = configuration;
            }

            [HttpPost("login")]
            public IActionResult Login([FromBody] LoginInput login)
            {
                try
                {
                    
                    var user = _context.User.Find(u => u.Email == login.Email && u.Password == login.Password).FirstOrDefault();

                    if (user == null)
                        return Unauthorized();

                    var token = GenerateJwtToken(user);
                    return Ok(new { token, userId = user._id });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while logging in", error = ex.Message });
                }
            }

            [HttpPost("google-login-client")]
            public async Task<IActionResult> GoogleLoginClient([FromBody] GoogleLogin input)
            {
                try
                {
                    // Check if the user already exists in the database asynchronously
                    var existingUser = await _context.User.Find(u => u.Email == input.email).FirstOrDefaultAsync();

                    if (existingUser == null)
                    {
                        // If the user does not exist, create a new user
                        var newUser = new User
                        {
                            _id = "",
                            Email = input.email,
                            Username = input.given_name,
                            Role = "Client",
                            FirstName = input.given_name,
                            LastName = input.family_name,
                            ContactNumber = "",
                            BusinessPostCode = "",
                            City = "",
                            State = "",
                            PostalCode = "",
                            ProximityToWork = "",
                            RegisteredBusinessName = "",
                            AustralianBusinessNumber = "",
                            TypeofWork = "",
                            Status = "",
                            ReasonforDeclinedApplication = "",
                            AboutMeDescription = "",
                            Website = "",
                            FacebookAccount = "",
                            IGAccount = "",
                            Services = [], // Initialize the list
                            ProfilePicture = input.picture,
                            CertificationFilesUploaded = new List<string>(), // Initialize the list
                            AvailabilityToWork = "",
                            ActiveJobs = 0,
                            PendingOffers = 0,
                            CompletedJobs = 0,
                            EstimatedEarnings = 0,
                            CallOutRate = "",
                            PublishedAds = 0
                        };

                        // Insert the new user asynchronously
                        await _context.User.InsertOneAsync(newUser);
                        existingUser = newUser;
                    }

                    // Generate a JWT token for the user (assuming it's a synchronous method)
                    var token = GenerateJwtToken(existingUser);

                    // Return the token and user information
                    return Ok(new { token, userId = existingUser._id });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred during Google login", error = ex.Message });
                }
            }


            [HttpPost("google-login-tradie")]
            public async Task<IActionResult> GoogleLoginTradie([FromBody] GoogleLogin input)
            {
            try
            {
                // Check if the user already exists in the database asynchronously
                var existingUser = await _context.User.Find(u => u.Email == input.email).FirstOrDefaultAsync();

                if (existingUser == null)
                {
                    // If the user does not exist, create a new user
                    var newUser = new User
                    {
                        _id = "",
                        Email = input.email,
                        Username = input.given_name,
                        Role = "Tradie",
                        FirstName = input.given_name,
                        LastName = input.family_name,
                        ContactNumber = "",
                        BusinessPostCode = "",
                        City = "",
                        State = "",
                        PostalCode = "",
                        ProximityToWork = "",
                        RegisteredBusinessName = "",
                        AustralianBusinessNumber = "",
                        TypeofWork = "",
                        Status = "",
                        ReasonforDeclinedApplication = "",
                        AboutMeDescription = "",
                        Website = "",
                        FacebookAccount = "",
                        IGAccount = "",
                        Services = [], // Initialize the list
                        ProfilePicture = input.picture,
                        CertificationFilesUploaded = new List<string>(), // Initialize the list
                        AvailabilityToWork = "",
                        ActiveJobs = 0,
                        PendingOffers = 0,
                        CompletedJobs = 0,
                        EstimatedEarnings = 0,
                        CallOutRate = "",
                        PublishedAds = 0
                    };

                    // Insert the new user asynchronously
                    await _context.User.InsertOneAsync(newUser);
                    existingUser = newUser;
                }

                // Generate a JWT token for the user (assuming it's a synchronous method)
                var token = GenerateJwtToken(existingUser);

                // Return the token and user information
                return Ok(new { token, userId = existingUser._id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during Google login", error = ex.Message });
            }
        }
            private string GenerateJwtToken(User user)
                    {
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var secretKey = _configuration["JwtSettings:SecretKey"];
                        var issuer = _configuration["JwtSettings:Issuer"];
                        var audience = _configuration["JwtSettings:Audience"];

                        var key = Encoding.ASCII.GetBytes(secretKey);

                        var tokenDescriptor = new SecurityTokenDescriptor
                        {
                            Subject = new ClaimsIdentity(new[]
                            {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Role, user.Role)
                        }),
                            Expires = DateTime.UtcNow.AddHours(1),
                            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                            Issuer = issuer,
                            Audience = audience
                        };

                        var token = tokenHandler.CreateToken(tokenDescriptor);
                        return tokenHandler.WriteToken(token);
                    }

            [HttpPost("signup")]
            public IActionResult Signup([FromBody] User userSignupDto)
            {
                try
                {
                    // Check if the user already exists
                    var existingUser = _context.User.Find(u => u.Email == userSignupDto.Email).FirstOrDefault();
                    if (existingUser != null)
                        return BadRequest("User already exists");

                    // Add the new user to the database
                    _context.User.InsertOne(userSignupDto);

                    // Return a success response
                    return Ok(new { message = "Signup successful" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while signing up", error = ex.Message });
                }
            }


            private string HashPassword(string password)
                {
                    // Add your password hashing logic here, e.g., using BCrypt or another hashing algorithm.
                    return password; // Replace this with the actual hashed password.
                }

            //for OTP login and signup
            private void SendSmsOtp(string phoneNumber, string otp)
            {
                var acctsid = _configuration["Twilio:ACCOUNT_SID"];
                var token = _configuration["Twilio:AUTH_TOKEN"];

                TwilioClient.Init(acctsid, token);

                var message = MessageResource.Create(
                    body: $"Your OTP code is {otp}",
                    from: new PhoneNumber("Your Twilio Number"),
                    to: new PhoneNumber(phoneNumber)
                );
            }

            private async Task SendEmailOtp(string emailAddress, string otp)
            {
                var apikey = _configuration["SendGrid:API_KEY"];
                var email = _configuration["SendGrid:Email"];
                var appName = _configuration["SendGrid:AppName"];

                var client = new SendGridClient(apikey);
                var from = new EmailAddress(email, appName);
                var subject = "Your OTP Code";
                var to = new EmailAddress(emailAddress);
                var plainTextContent = $"Your OTP code is {otp}";
                var htmlContent = $"<strong>Your OTP code is {otp}</strong>";
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);
            }

            [HttpGet("userDetails")]
            public IActionResult GetProfileById(string id)
            {
                try
                {
                    var tradie = _context.User.Find(user => user._id == id).FirstOrDefault();
                    if (tradie == null)
                    {
                        return NotFound(new { message = "Profile not found" });
                    }
                    return Ok(tradie);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while getting your profile details", error = ex.Message });
                }
            }

            [HttpGet("allServices")]
            public IActionResult GetAllServices()
            {
                try
                {
                    var services = _context.Services.Find(services => true).ToList(); // Fetch all users from MongoDB
                    return Ok(services);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while retrieving services", error = ex.Message });
                }
            }

            [HttpGet("JobPostDetails")]
            public IActionResult GetJobPostDetails([FromBody] OwnProfile serviceProfile)
            {
                try
                {
                    var service = _context.Services.Find(service => service._id == serviceProfile.ID).FirstOrDefault();
                    if (service == null)
                    {
                        return NotFound(new { message = "Job Ad Post not found" });
                    }
                    return Ok(service);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while getting job post", error = ex.Message });
                }
            }
            [HttpGet("FilteredServices")]
            public IActionResult GetFilteredServices([FromBody] JobAdPostFilter filterInput)
            {
                try
                {
                    var filterBuilder = Builders<Services>.Filter;
                    var filter = filterBuilder.Empty; // Start with an empty filter

                    // Filter by Postcode
                    if (!string.IsNullOrEmpty(filterInput.Postcode))
                    {
                        filter &= filterBuilder.Eq(s => s.BusinessPostcode, filterInput.Postcode);
                    }

                    // Filter by JobCategory (if multiple categories are provided)
                    if (!(filterInput.JobCategories.Any(category => string.IsNullOrWhiteSpace(category))) && filterInput.JobCategories.Count > 0)
                    {
                        filter &= filterBuilder.In(s => s.JobCategory, filterInput.JobCategories);
                    }

                    // Filter by Keywords (match JobAdTitle using a case-insensitive regex)
                    if (!string.IsNullOrEmpty(filterInput.Keywords))
                    {
                        var regexFilter = new BsonRegularExpression(filterInput.Keywords, "i"); // Case-insensitive search
                        filter &= filterBuilder.Regex(s => s.JobAdTitle, regexFilter);
                    }


                    // Filter by PricingStartsAt (range between min and max)
                    if (filterInput.PricingStartsMax > filterInput.PricingStartsMin)
                    {
                        filter &= filterBuilder.Eq(s => s.PricingOption, "Hourly");
                        filter &= filterBuilder.Gte(s => s.PricingStartsAt, filterInput.PricingStartsMin.ToString()) &
                                  filterBuilder.Lte(s => s.PricingStartsAt, filterInput.PricingStartsMax.ToString());
                    }

                    var filteredServices = _context.Services.Find(filter).ToList();

                    var userIds = filteredServices.Select(s => s.UserID).Distinct().ToList();


                    var userFilterBuilder = Builders<User>.Filter;
                    var userFilter = userFilterBuilder.In(u => u._id, userIds);

                    if (filterInput.CallOutRateMax > filterInput.CallOutRateMin)
                    {
                        userFilter &= userFilterBuilder.Gte(u => u.CallOutRate, filterInput.CallOutRateMin.Value.ToString()) &
                                      userFilterBuilder.Lte(u => u.CallOutRate, filterInput.CallOutRateMin.Value.ToString());
                    }

                    // Filter by ProximityToWork (min and max range)
                    if (filterInput.ProximityToWorkMax > filterInput.ProximityToWorkMin)
                    {
                        userFilter &= userFilterBuilder.Gte(u => u.ProximityToWork, filterInput.ProximityToWorkMin.Value.ToString()) &
                                      userFilterBuilder.Lte(u => u.ProximityToWork, filterInput.ProximityToWorkMax.Value.ToString());
                    }

                    // Filter by AvailabilityToWork (multiple answers)
                    if (!String.IsNullOrEmpty(filterInput.AvailabilityToWork[0]) && filterInput.AvailabilityToWork.Count > 0)
                    {
                        userFilter &= userFilterBuilder.In(u => u.AvailabilityToWork, filterInput.AvailabilityToWork);
                    }

                    // Fetch the filtered users that match the user filters
                    var filteredUsers = _context.User.Find(userFilter).ToList();
                    var finalUserIds = filteredUsers.Select(u => u._id).ToList();

                    // Step 4: Filter the services again based on the final list of UserIDs from the User filter
                    var finalServices = filteredServices.Where(s => finalUserIds.Contains(s.UserID)).ToList();

                    return Ok(filteredServices);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while retrieving services", error = ex.Message });
                }
            }

    }
    
}
