using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using BrodClientAPI.Data;
using BrodClientAPI.Models;
using MongoDB.Driver;
using Amazon;
using Amazon.Pinpoint;
using Amazon.Pinpoint.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Caching.Memory;

namespace BrodClientAPI.Controller
{
        [ApiController]
        [Route("api/[controller]")]
        public class AuthController : ControllerBase
        {
            private readonly ApiDbContext _context;
            private readonly IConfiguration _configuration;
            private readonly AmazonPinpointClient _pinpointClient;
            private readonly IMemoryCache _cache;

            public AuthController(ApiDbContext context, IConfiguration configuration, IMemoryCache cache)
                {
                _context = context;
                _configuration = configuration;
                var awsAccessKey = _configuration["AWS:AccessKey"];
                var awsSecretKey = _configuration["AWS:SecretKey"];
                var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
                _pinpointClient = new AmazonPinpointClient(credentials, RegionEndpoint.APSoutheast2);
                _cache = cache;
               }

            [HttpGet("allUsers")]
            public async Task<IActionResult> GetAllUsers()
            {
                try
                {
                    var users = await _context.User.Find(_ => true).ToListAsync();
                    return Ok(users);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while retrieving users", error = ex.Message });
                }
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

            [HttpPost("sso-login-common")]
            public async Task<IActionResult> GoogleLoginCommon([FromBody] GoogleLogin input)
            {
                try
                {
                    // Check if the user already exists in the database asynchronously
                    var existingUser = await _context.User.Find(u => u.Email == input.email).FirstOrDefaultAsync();

                    if (existingUser == null)
                    {
                        return Ok("Please sign up as a new user");
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
            //for apple and google login for client
            [HttpPost("sso-client")]
            public async Task<IActionResult> GoogleLoginClient([FromBody] GoogleLogin input)
            {
                try
                {
                    var password = PasswordGenerator.GeneratePassword();

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
                            Password= password,
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

                    
                        // Create the request to send the email message
                        var request = new SendMessagesRequest
                        {
                            ApplicationId = _configuration["AWS:PinpointAppId"],
                            MessageRequest = new MessageRequest
                            {
                                Addresses = new Dictionary<string, AddressConfiguration>
                                {
                                    { input.email, new AddressConfiguration { ChannelType = ChannelType.EMAIL } }
                                },
                                MessageConfiguration = new DirectMessageConfiguration
                                {
                                    EmailMessage = new EmailMessage
                                    {
                                        FromAddress = _configuration["AWS:FromEmailAddress"],
                                        SimpleEmail = new SimpleEmail
                                        {
                                            Subject = new SimpleEmailPart { Data = "Important: Your Temporary Password for Brod Client" },
                                            HtmlPart = new SimpleEmailPart
                                            {
                                                Data = $@"
                                                    <html>
                                                        <body style='font-family: Arial, sans-serif;'>
                                                            <h2 style='color: #000000;'>Official Notification</h2>
                                                            <p>Dear User,</p>
                                                            <p>Your temporary password for Brod Client is: <strong>{password}</strong></p>
                                                            <p>Please use this password to log in and change it to a new one as soon as possible.</p>
                                                            <p>If you did not request this password, please contact our support team immediately.</p>
                                                            <p>Thank you,</p>
                                                            <p><em>Brod Client Support Team</em></p>
                                                        </body>
                                                    </html>"
                                            },
                                            TextPart = new SimpleEmailPart
                                            {
                                                Data = $@"
                                                    Official Notification

                                                    Dear User,

                                                    Your temporary password for Brod Client is: {password}

                                                    Please use this password to log in and change it to a new one as soon as possible.

                                                    If you did not request this password, please contact our support team immediately.

                                                    Thank you,

                                                    Brod Client Support Team"
                                            }
                                        }
                                    }
                                }
                            }
                        };

                        // Send the email through Pinpoint
                        var response = await _pinpointClient.SendMessagesAsync(request);

                        // Insert the new user asynchronously
                        await _context.User.InsertOneAsync(newUser);
                        existingUser = await _context.User.Find(u => u.Email == input.email).FirstOrDefaultAsync(); ;
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
            //for apple and google login for tradie
            [HttpPost("sso-tradie")]
            public async Task<IActionResult> GoogleLoginTradie([FromBody] GoogleLogin input)
            {
            try
            {
                var password = PasswordGenerator.GeneratePassword();

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
                        Password = password,
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
                        Status = "New",
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


                    // Create the request to send the email message
                    var request = new SendMessagesRequest
                    {
                        ApplicationId = _configuration["AWS:PinpointAppId"],
                        MessageRequest = new MessageRequest
                        {
                            Addresses = new Dictionary<string, AddressConfiguration>
                    {
                        { input.email, new AddressConfiguration { ChannelType = ChannelType.EMAIL } }
                    },
                            MessageConfiguration = new DirectMessageConfiguration
                            {
                                EmailMessage = new EmailMessage
                                {
                                    FromAddress = _configuration["AWS:FromEmailAddress"],
                                    SimpleEmail = new SimpleEmail
                                    {
                                        Subject = new SimpleEmailPart { Data = "Important: Your Temporary Password for Brod Client" },
                                        HtmlPart = new SimpleEmailPart
                                        {
                                            Data = $@"
                                                    <html>
                                                        <body style='font-family: Arial, sans-serif;'>
                                                            <h2 style='color: #000000;'>Official Notification</h2>
                                                            <p>Dear User,</p>
                                                            <p>Your temporary password for Brod Client is: <strong>{password}</strong></p>
                                                            <p>You can login using email and password or still your google login.</p>
                                                            <p>If you did not request this password, please contact our support team immediately.</p>
                                                            <p>Thank you,</p>
                                                            <p><em>Brod Client Support Team</em></p>
                                                        </body>
                                                    </html>"
                                        },
                                        TextPart = new SimpleEmailPart
                                        {
                                            Data = $@"
                                                    Official Notification

                                                    Dear User,

                                                    Your temporary password for Brod Client is: {password}

                                                    You can login using email and password or still your google login.

                                                    If you did not request this password, please contact our support team immediately.

                                                    Thank you,

                                                    Brod Client Support Team"
                                        }
                                    }
                                }
                            }
                        }
                    };

                    // Send the email through Pinpoint
                    var response = await _pinpointClient.SendMessagesAsync(request);

                    // Insert the new user asynchronously
                    await _context.User.InsertOneAsync(newUser);

                    existingUser = await _context.User.Find(u => u.Email == input.email).FirstOrDefaultAsync();
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
                                        Subject = new SimpleEmailPart { Data = "Important: Your OTP Code" },
                                        HtmlPart = new SimpleEmailPart
                                        {Data = $@"
                                                    <html>
                                                        <body>
                                                            <h2 style='color: #000000;'>Official Notification</h2>
                                                            <p>Dear User,</p>
                                                            <p>Your One-Time Password (OTP) code is: <strong>{otp}</strong></p>
                                                            <p>Please use this code to complete your verification process. If you did not request this code, please contact our support team immediately.</p>
                                                            <p>Thank you,</p>
                                                            <p><em>Brod Client</em></p>
                                                        </body>
                                                    </html>"},
                                        TextPart = new SimpleEmailPart
                                                                                {
                                                                                    Data = $@"
                                                                                            Official Notification

                                                                                            Dear User,

                                                                                            Your One-Time Password (OTP) code is: {otp}

                                                                                            Please use this code to complete your verification process. If you did not request this code, please contact our support team immediately.

                                                                                            Thank you,

                                                                                            Stefan"
                                                                                }
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

            [HttpPost("email-verify-otp")]
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
                    var cacheKey = "allServices";
                    if (!_cache.TryGetValue(cacheKey, out List<Services>? services))
                    {
                        services = await _context.Services.Find(service => service.IsActive == true).ToListAsync();

                        var cacheEntryOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                            SlidingExpiration = TimeSpan.FromMinutes(2)
                        };

                        _cache.Set(cacheKey, services, cacheEntryOptions);
                    }

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

                    var rating = await _context.Rating.Find(rating => rating.tradieId == service.ThumbnailImage).ToListAsync();
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

                    var jobPostWithRatings = new JobPostWithRatings
                    {
                        service = service,
                        ratings = rating,
                        TotalRating = totalRate
                    };

                return Ok(jobPostWithRatings);
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
                    // Fetch active services
                    var services = await _context.Services.Find(service => service.IsActive == true).ToListAsync();

                    // Apply additional filters
                    if (!string.IsNullOrEmpty(filterInput.Postcode))
                    {
                        services = services.Where(s => s.BusinessPostcode == filterInput.Postcode).ToList();
                    }

                    if (filterInput.JobCategories.Any(category => !string.IsNullOrWhiteSpace(category)) && filterInput.JobCategories.Count > 0)
                    {
                        services = services.Where(s => filterInput.JobCategories.Contains(s.JobCategory)).ToList();
                    }

                    if (!string.IsNullOrEmpty(filterInput.Keywords))
                    {
                        var keyword = filterInput.Keywords.ToLower();
                        services = services.Where(s =>
                            s.JobAdTitle.ToLower().Contains(keyword) ||
                            s.DescriptionOfService.ToLower().Contains(keyword) ||
                            s.JobCategory.ToLower().Contains(keyword)).ToList();
                    }

                    if (filterInput.PricingStartsMax > filterInput.PricingStartsMin)
                    {
                        services = services.Where(s =>
                            s.PricingOption == "Hourly" &&
                            decimal.TryParse(s.PricingStartsAt, out var price) &&
                            price >= filterInput.PricingStartsMin &&
                            price <= filterInput.PricingStartsMax).ToList();
                    }

                    var userIds = services.Select(s => s.UserID).Distinct().ToList();

                    var users = await _context.User.Find(u => userIds.Contains(u._id)).ToListAsync();

                    if (filterInput.CallOutRateMax > filterInput.CallOutRateMin)
                    {
                        users = users.Where(u =>
                            decimal.TryParse(u.CallOutRate, out var rate) &&
                            rate >= filterInput.CallOutRateMin &&
                            rate <= filterInput.CallOutRateMax).ToList();
                    }

                    if (filterInput.ProximityToWorkMax > filterInput.ProximityToWorkMin)
                    {
                        users = users.Where(u =>
                            decimal.TryParse(u.ProximityToWork, out var proximity) &&
                            proximity >= filterInput.ProximityToWorkMin &&
                            proximity <= filterInput.ProximityToWorkMax).ToList();
                    }

                    if (!string.IsNullOrEmpty(filterInput.AvailabilityToWork[0]) && filterInput.AvailabilityToWork.Count > 0)
                    {
                        users = users.Where(u => filterInput.AvailabilityToWork.Contains(u.AvailabilityToWork)).ToList();
                    }

                    var finalUserIds = users.Select(u => u._id).ToList();
                    var finalServices = services.Where(s => finalUserIds.Contains(s.UserID)).ToList();

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

            [HttpPost("GetNotifications")]
            public async Task<IActionResult> GetNotifications(string userId)
            {
                try
                {
                    var notifVal = await _context.Notification
                                                    .Find(notif => notif.userID == userId && notif.isRead == false)
                                                    .ToListAsync();
                    if (notifVal == null)
                    {
                        return Ok("No new notification!");
                    }

                    foreach (var notif in notifVal) {
                        var filter = Builders<Notification>.Filter.Eq(n => n._id, notif._id);
                        var update = Builders<Notification>.Update.Set(n => n.isRead, true);
                        await _context.Notification.UpdateOneAsync(filter, update);
                    }

                    return Ok(notifVal);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while updating the notification", error = ex.Message });
                }
            }

            [HttpPost("GetNotificationsNoUpdate")]
            public async Task<IActionResult> GetNotificationsNoUpdate(string userId)
            {
                try
                {
                    var notifVal = await _context.Notification
                                                    .Find(notif => notif.userID == userId && notif.isRead == false)
                                                    .ToListAsync();
                    if (notifVal == null)
                    {
                        return Ok("No new notification!");
                    }

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

            [HttpPost("Client-AddMessage")]
            public async Task<IActionResult> ClientAddMessage([FromBody] ClientMessage message)
            {
                try
                {
                    var tradie = await _context.User.Find(user => user._id == message.TradieId).FirstOrDefaultAsync();
                    if (tradie == null)
                    {
                        return NotFound();
                    }
                    message.Picture = tradie.ProfilePicture;
                    message.Tradielocation = $"{tradie.City},{tradie.State} {tradie.PostalCode}";
                    message.TradieName = $"{tradie.FirstName} {tradie.LastName}";

                    await _context.ClientMessage.InsertOneAsync(message);

                return Ok(message); 
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while adding message", error = ex.Message });
                }
            }

            [HttpPost("Tradie-AddMessage")]
            public async Task<IActionResult> TradieAddMessage([FromBody] TradieMessage message)
            {
                try
                {
                    var client = await _context.User.Find(user => user._id == message.ClientId).FirstOrDefaultAsync();
                    if (client == null)
                    {
                        return NotFound();
                    }
                    message.Picture = client.ProfilePicture;
                    message.Clientlocation = $"{client.City},{client.State} {client.PostalCode}";
                    message.ClientName = $"{client.FirstName} {client.LastName}";

                    await _context.TradieMessage.InsertOneAsync(message);

                    return Ok(message);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while adding message", error = ex.Message });
                }
            }

            [HttpPost("Client-GetAll-Messages")]
            public async Task<IActionResult> ClientGetAllMessages([FromBody] GetMessage getMessage)
            {
                try
                {
                    var filter = Builders<ClientMessage>.Filter.Eq(mess => mess.ClientId, getMessage.ClientId);

                    var messages = await _context.ClientMessage
                        .Find(filter)
                        .SortBy(mess => mess.TimeStamp)
                        .ToListAsync()
                        .ContinueWith(task => task.Result
                            .GroupBy(mess => mess.TradieId)
                            .Select(group => group.First())
                            .ToList());

                    return Ok(messages);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while gettting messages", error = ex.Message });
                }
            }

            [HttpPost("GetMessages-ByID")]
            public async Task<IActionResult> GetMessagesById([FromBody] GetMessage getMessage)
            {
                try
                {
                    var filter = Builders<ClientMessage>.Filter.And(
                        Builders<ClientMessage>.Filter.Eq(mess => mess.ClientId, getMessage.ClientId),
                        Builders<ClientMessage>.Filter.Eq(mess => mess.TradieId, getMessage.TradieId)
                    );

                    var messByClient = await _context.ClientMessage
                        .Find(filter)
                        .SortBy(mess => mess.TimeStamp)
                        .ToListAsync();

                    var filter2 = Builders<TradieMessage>.Filter.And(
                        Builders<TradieMessage>.Filter.Eq(mess => mess.ClientId, getMessage.ClientId),
                        Builders<TradieMessage>.Filter.Eq(mess => mess.TradieId, getMessage.TradieId)
                    );

                    var messByTradie = await _context.TradieMessage
                        .Find(filter2)
                        .SortBy(mess => mess.TimeStamp)
                        .ToListAsync();

                    var combinedMessages = new CombinedMessages
                    {
                        ClientMessages = new List<ClientMessage>(),
                        TradieMessages = new List<TradieMessage>()
                    };


                    if (messByClient != null && messByClient.Count > 0)
                    {
                        foreach (var mess in messByClient)
                        {
                            combinedMessages.ClientMessages.Add(mess);
                        }
                    }

                    if (messByTradie != null && messByTradie.Count > 0)
                    {
                        foreach (var mess in messByTradie)
                        {
                            combinedMessages.TradieMessages.Add(mess);
                        }
                    }
                    return Ok(combinedMessages);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while gettting messages", error = ex.Message });
                }
            }

            [HttpPost("Tradie-GetAll-Messages")]
            public async Task<IActionResult> TradieGetAllMessages([FromBody] GetMessage getMessage)
            {
                try
                {
                    var filter = Builders<TradieMessage>.Filter.Eq(mess => mess.TradieId, getMessage.TradieId);

                    var messages = await _context.TradieMessage
                                    .Find(filter)
                                    .SortBy(mess => mess.TimeStamp)
                                    .ToListAsync()
                                    .ContinueWith(task => task.Result
                                        .GroupBy(mess => mess.ClientId)
                                        .Select(group => group.First())
                                        .ToList());

                    return Ok(messages);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "An error occurred while gettting messages", error = ex.Message });
                }
            }

            [HttpPut("Reactivate")]
            public async Task<IActionResult> ReactivateUser(string userId)
            {
                var user = await _context.User.Find(user => user._id == userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound();
                }

                var update = Builders<User>.Update
                .Set(u => u.isSuspended, false)
                .Set(u => u.weeksSuspended, 0)
                .Set(u => u.suspensionStartDate, null);

                await _context.User.UpdateOneAsync(user => user._id == userId, update);

                return Ok(new { message = "User successfully reactivated" });
            }
            [HttpPut("ChangePassword")]
            public async Task<IActionResult> ChangePassword(ChangePassword changePass)
            {
                var user = await _context.User.Find(user => user.Email == changePass.Email).FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound();
                }
                
                if(changePass.OldPassword == user.Password){
                    await _context.User.UpdateOneAsync(
                        Builders<User>.Filter.Eq(u => u.Email, changePass.Email),
                        Builders<User>.Update.Set(u => u.Password, changePass.NewPassword)
                    );
                    return Ok(new { message = "User successfully changed password" });
                }


                return Ok(new { message = "Invalid Password" });
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
    }

}
