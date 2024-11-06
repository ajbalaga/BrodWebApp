﻿using System.IdentityModel.Tokens.Jwt;
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
using Amazon;
using Amazon.Pinpoint;
using Amazon.Pinpoint.Model;
using Amazon.Runtime;
using Microsoft.VisualBasic;

namespace BrodClientAPI.Controller
{
        [ApiController]
        [Route("api/[controller]")]
        public class AuthController : ControllerBase
        {
            private readonly ApiDbContext _context;
            private readonly IConfiguration _configuration;
            private readonly AmazonPinpointClient _pinpointClient;

            public AuthController(ApiDbContext context, IConfiguration configuration)
                {
                _context = context;
                _configuration = configuration;
                var awsAccessKey = _configuration["AWS:AccessKey"];
                var awsSecretKey = _configuration["AWS:SecretKey"];
                var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
                _pinpointClient = new AmazonPinpointClient(credentials, RegionEndpoint.APSoutheast1);
        }

            [HttpPost("login")]
            public async Task<IActionResult> Login([FromBody] LoginInput login)
            {
                try
                {
                    var user = await _context.User.Find(u => u.Email == login.Email && u.Password == login.Password).FirstOrDefaultAsync();

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
            public async Task<IActionResult> Signup([FromBody] User userSignupDto)
            {
                try
                {
                    // Check if the user already exists
                    var existingUser = await _context.User.Find(u => u.Email == userSignupDto.Email).FirstOrDefaultAsync();
                    if (existingUser != null)
                        return BadRequest("User already exists");

                    // Add the new user to the database
                    await _context.User.InsertOneAsync(userSignupDto);

                    // Return a success response
                    return Ok(new { message = "Signup successful" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while signing up", error = ex.Message });
                }
            }


            [HttpPost("sms-otp")]
            public async Task<IActionResult> SendSMSOTP(string phoneNumber)
            {
                try
                {
                    var otp = new Random().Next(100000, 999999).ToString();

                    var otpSms = new OTPSMS
                    {
                        phoneNumber = phoneNumber,
                        OTP = otp,
                        expirationMin = DateTime.Now.AddMinutes(5),
                    };

                    _context.OtpSMS.InsertOne(otpSms);

                    var request = new SendMessagesRequest
                    {
                        ApplicationId = _configuration["AWS:PinpointAppId"],
                        MessageRequest = new MessageRequest
                        {
                            Addresses = new Dictionary<string, AddressConfiguration>
                            {
                                { phoneNumber, new AddressConfiguration { ChannelType = "SMS" } }
                            },
                            MessageConfiguration = new DirectMessageConfiguration
                            {
                                SMSMessage = new SMSMessage
                                {
                                    Body = $"Your OTP code is {otp}",
                                    MessageType = "TRANSACTIONAL"
                                }
                            }
                        }
                    };

                    var response = await _pinpointClient.SendMessagesAsync(request);

                    return Ok("OTP sent successfully.");
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error sending OTP: {ex.Message}");
                }
            }

            [HttpPost("email-otp")]
            public async Task<IActionResult> SendEmailOTP(string email)
            {
                try
                {
                    // Generate OTP
                    var otp = new Random().Next(100000, 999999).ToString();

                    // Store OTP in database with expiration time
                    var otpEmail = new OTPEMAIL
                    {
                        email = email,
                        OTP = otp,
                        expirationMin = DateTime.Now.AddMinutes(5)
                    };

                    _context.OtpEmail.InsertOne(otpEmail);

                    // Create the request to send the email message
                    var request = new SendMessagesRequest
                    {
                        ApplicationId = _configuration["AWS:PinpointAppId"],
                        MessageRequest = new MessageRequest
                        {
                            Addresses = new Dictionary<string, AddressConfiguration>
                    {
                        { email, new AddressConfiguration { ChannelType = ChannelType.EMAIL } }
                    },
                            MessageConfiguration = new DirectMessageConfiguration
                            {
                                EmailMessage = new EmailMessage
                                {
                                    FromAddress = _configuration["AWS:FromEmailAddress"],
                                    SimpleEmail = new SimpleEmail
                                    {
                                        Subject = new SimpleEmailPart { Data = "Your OTP Code" },
                                        HtmlPart = new SimpleEmailPart { Data = $"<h1>Your OTP code is {otp}</h1>" },
                                        TextPart = new SimpleEmailPart { Data = $"Your OTP code is {otp}" }
                                    }
                                }
                            }
                        }
                    };

                    // Send the email through Pinpoint
                    var response = await _pinpointClient.SendMessagesAsync(request);

                    // Check if the email was successfully sent
                    if (response.MessageResponse.Result[email].StatusCode == 200)
                    {
                        return Ok("OTP sent successfully.");
                    }
                    else
                    {
                        return BadRequest("Failed to send OTP.");
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error sending OTP: {ex.Message}");
                }
            }

            [HttpPost("sms-email-otp")]
            public async Task<IActionResult> VerifyEmailOtp(string email, string userEnteredOtp)
            {
                try
                {
                    var otpSms = await _context.OtpEmail.Find(x => x.email == email && x.OTP == userEnteredOtp).FirstOrDefaultAsync();
                    if (otpSms == null || DateTime.UtcNow > otpSms.expirationMin)
                    {
                        return BadRequest("OTP has expired.");
                    }
                    return Ok("OTP verified successfully.");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }


            [HttpPost("sms-verify-otp")]
            public async Task<IActionResult> VerifySMSOtp(string phoneNumber, string userEnteredOtp)
            {
                try
                {
                    var otpSms = await _context.OtpSMS.Find(x => x.phoneNumber == phoneNumber && x.OTP == userEnteredOtp).FirstOrDefaultAsync();
                    if (otpSms == null || DateTime.UtcNow > otpSms.expirationMin)
                    {
                        return BadRequest("OTP has expired.");
                    }
                    return Ok("OTP verified successfully.");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }


            [HttpGet("userDetails")]
            public async Task<IActionResult> GetProfileById(string id)
            {
                try
                {
                    var user = await _context.User.Find(user => user._id == id).FirstOrDefaultAsync();
                    if (user == null)
                    {
                        return NotFound();
                    }
                    return Ok(user);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while getting your profile details", error = ex.Message });
                }
            }


            // Fetch tradie details by ID
            [HttpPost("tradieProfileByID")]
            public async Task<IActionResult> GetTradieById([FromBody] OwnProfile getTradieProfile)
            {
                var tradie = await _context.User.Find(user => user._id == getTradieProfile.ID && user.Role == "Tradie").FirstOrDefaultAsync();
                if (tradie == null)
                {
                    return NotFound();
                }

                var rating = await _context.Rating.Find(rating => rating.tradieId == getTradieProfile.ID).ToListAsync();
                if (rating == null)
                {
                    return NotFound();
                }

                var ratingVal = 0;
                var count = 0;
                foreach (var ratingItem in rating)
                {
                    ratingVal += ratingItem.rating;
                    count++;
                }
                var totalRate = count > 0 ? ratingVal / count : 0;

                var model = new TradieProfile
                {
                    user = tradie,
                    ratings = rating,
                    TotalRating = totalRate
                };
                return Ok(model);
            }


            [HttpGet("allServices")]
            public async Task<IActionResult> GetAllServices()
            {
                try
                {
                    var services = await _context.Services.Find(service => service.IsActive == true).ToListAsync();
                    return Ok(services);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while retrieving services", error = ex.Message });
                }
            }


            [HttpPost("JobPostDetails")]
            public async Task<IActionResult> GetJobPostDetails([FromBody] OwnProfile serviceProfile)
            {
                try
                {
                    var service = await _context.Services.Find(service => service._id == serviceProfile.ID).FirstOrDefaultAsync();
                    if (service == null)
                    {
                        return NotFound();
                    }
                    return Ok(service);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while getting job post", error = ex.Message });
                }
            }


            [HttpPost("FilteredServices")]
            public async Task<IActionResult> GetFilteredServices([FromBody] JobAdPostFilter filterInput)
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

                    var filteredServices = await _context.Services.Find(filter).ToListAsync();

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
                    var filteredUsers = await _context.User.Find(userFilter).ToListAsync();
                    var finalUserIds = filteredUsers.Select(u => u._id).ToList();

                    // Step 4: Filter the services again based on the final list of UserIDs from the User filter
                    var finalServices = filteredServices.Where(s => finalUserIds.Contains(s.UserID)).ToList();

                    return Ok(finalServices);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while retrieving services", error = ex.Message });
                }
            }


            [HttpPost("AddNotification")]
            public async Task<IActionResult> AddNotification([FromBody] Notification notification)
            {
                try
                {
                    var user = await _context.User.Find(user => user._id == notification.userID).FirstOrDefaultAsync();
                    if (user == null)
                    {
                        return NotFound();
                    }

                    await _context.Notification.InsertOneAsync(notification);

                    return Ok(notification);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while adding notification", error = ex.Message });
                }
            }


            [HttpPut("ReadNotification")]
            public async Task<IActionResult> ReadNotification([FromBody] ReadNotif readNotif)
                {
                    try
                    {
                        var notifVal = await _context.Notification.Find(notif => notif._id == readNotif.NotificationId).FirstOrDefaultAsync();
                        if (notifVal == null)
                        {
                            return NotFound();
                        }
                        notifVal.isRead = true;

                        await _context.Notification.ReplaceOneAsync(notif => notif._id == readNotif.NotificationId, notifVal);

                        return Ok(notifVal);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { message = "An error occurred while updating the notification", error = ex.Message });
                    }
                }

            [HttpPost("AddMessage")]
            public async Task<IActionResult> AddMessage([FromBody] Messages message)
            {
                try
                {

                    await _context.Messages.InsertOneAsync(message);

                    return Ok(message);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while adding message", error = ex.Message });
                }
            }


            [HttpPost("GetMessages")]
            public async Task<IActionResult> GetMessage([FromBody] GetMessage getMessage)
            {
                try
                {
                var filter =    Builders<Messages>.Filter.And(
                                Builders<Messages>.Filter.Eq(mess => mess.ClientId, getMessage.ClientId),
                                Builders<Messages>.Filter.Eq(mess => mess.TradieId, getMessage.TradieId)
                            );

                var messages = await _context.Messages
                    .Find(filter)
                    .SortBy(mess => mess.TimeStamp)
                    .ToListAsync();

                return Ok(messages);
            }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while gettting messages", error = ex.Message });
                }
            }


    }

}
