using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using BrodClientAPI.Data;
using BrodClientAPI.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;
using BrodClientAPI.Service;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Diagnostics;

namespace BrodClientAPI.Controller
{
        [ApiController]
        [Route("api/[controller]")]
        public class AuthController : ControllerBase
        {
            private readonly ApiDbContext _context;
            private readonly IConfiguration _configuration;
            private readonly IMemoryCache _cache;
            private readonly TwilioService _twilioService;

            public AuthController(ApiDbContext context, IConfiguration configuration, IMemoryCache cache, TwilioService twilioService)
            {
                _context = context;
                _configuration = configuration;                
                _cache = cache;
                _twilioService = twilioService;
            }

            [HttpGet("allUsers")]
            public async Task<IActionResult> GetAllUsers()
            {
                try
                {
                    var users = _context.User.AsQueryable().ToList();
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
                            Password = password,
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
                            Services = new List<string>(), // Initialize the list
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

                        // SendGrid API Key from configuration
                        var sendGridApiKey = _configuration["SendGrid:ApiKey"];
                        var fromEmailAddress = _configuration["SendGrid:FromEmailAddress"];

                        // Initialize SendGrid client
                        var client = new SendGrid.SendGridClient(sendGridApiKey);

                        // Prepare email message
                        var from = new SendGrid.Helpers.Mail.EmailAddress(fromEmailAddress, "Brod Support Team");
                        var to = new SendGrid.Helpers.Mail.EmailAddress(input.email);
                        var subject = "Important: Your Temporary Password for Brod";
                        var plainTextContent = $@"
                    Dear User,

                    Your temporary password for Brod is: {password}

                    Please use this password to log in and change it to a new one as soon as possible.

                    If you did not request this password, please contact our support team immediately.

                    Best regards,
                    BROD Team";

                        var htmlContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <p>Dear User,</p>
                            <br><p>Your temporary password for Brod is: <strong>{password}</strong></p>
                            <p>Please use this password to log in and change it to a new one as soon as possible.</p>
                            <p>If you did not request this password, please contact our support team immediately.</p>
                            <br><p>Best regards,</p>
                            <p><em>BROD Team</em></p>
                        </body>
                    </html>";

                        var msg = SendGrid.Helpers.Mail.MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                        // Send the email
                        var response = await client.SendEmailAsync(msg);

                        if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                        {
                            return BadRequest("Failed to send the temporary password email.");
                        }

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
                            Services = new List<string>(), // Initialize the list
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

                        // SendGrid API Key from configuration
                        var sendGridApiKey = _configuration["SendGrid:ApiKey"];
                        var fromEmailAddress = _configuration["SendGrid:FromEmailAddress"];

                        // Initialize SendGrid client
                        var client = new SendGrid.SendGridClient(sendGridApiKey);

                        // Prepare email message
                        var from = new SendGrid.Helpers.Mail.EmailAddress(fromEmailAddress, "Brod Support Team");
                        var to = new SendGrid.Helpers.Mail.EmailAddress(input.email);
                        var subject = "Important: Your Temporary Password for Brod";
                        var plainTextContent = $@"
                    Dear User,

                    Your temporary password for Brod is: {password}

                    You can login using email and password or your Google login.

                    If you did not request this password, please contact our support team immediately.

                    Best regards,

                    BROD Team";

                        var htmlContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <p>Dear User,</p>
                            <br><p>Your temporary password for Brod is: <strong>{password}</strong></p>
                            <p>You can login using email and password or your Google login.</p>
                            <p>If you did not request this password, please contact our support team immediately.</p>
                            
                            <br><p>Best regards,</p>
                            <p><em>BROD Team</em></p>
                        </body>
                    </html>";

                        var msg = SendGrid.Helpers.Mail.MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                        // Send the email
                        var response = await client.SendEmailAsync(msg);

                        if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                        {
                            return BadRequest("Failed to send the temporary password email.");
                        }

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
                        return BadRequest("Email already used!");

                    // Add the new user to the database
                    await _context.User.InsertOneAsync(userSignupDto);

                    if (userSignupDto.Role.ToLower()=="tradie") {
                    
                    // SendGrid API Key from configuration
                    var sendGridApiKey = _configuration["SendGrid:ApiKey"];
                    var fromEmailAddress = _configuration["SendGrid:FromEmailAddress"];

                    var client = new SendGrid.SendGridClient(sendGridApiKey);
                    var subject = "";
                    var plainTextContent = "";
                    var htmlContent = "";
                        subject = "Your Account is Under Review";
                        plainTextContent = $@"
                        Hi {userSignupDto.FirstName},

                       Thanks for signing up with BROD. Your account is currently under review, which typically takes 3-5 business days.

                        During this time, you won't be able to create job ads. Once your account is approved, you'll be able to start posting and managing your listings.

                        If you have any questions, feel free to contact us at support@brod.com.au.

                        Thank you for your patience.

                        Best regards,
                        BROD Team";
                        
                        htmlContent = $@"
                        <html>
                            <body style='font-family: Arial, sans-serif; color: #000000;'>
                                <p>Hi {userSignupDto.FirstName},</p>
                                <br><p>Thanks for signing up with BROD. Your account is currently under review, which typically takes 3-5 business days.</p>
                                <br><p>During this time, you won't be able to create job ads. Once your account is approved, you'll be able to start posting and managing your listings.</p>
                                <br><p>If you have any questions, feel free to contact us at support@brod.com.au.</p>
                                <br><p>Thank you for your patience.</p>
                                <br><p>Best regards,</p>
                                <p><strong>BROD Team</strong></p>
                            </body>
                        </html>";
                        

                        // Prepare email message
                        var from = new SendGrid.Helpers.Mail.EmailAddress(fromEmailAddress, "Brod System");
                        var to = new SendGrid.Helpers.Mail.EmailAddress(userSignupDto.Email);

                        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                }

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
               
                    var accountSid2 = _configuration["Twilio:AccountSID"];
                    var authToken2 = _configuration["Twilio:AuthToken"];
                    var fromPhoneNumber = _configuration["Twilio:FromPhoneNumber"];

                    TwilioClient.Init(accountSid2, authToken2);
                    var messageOptions = new CreateMessageOptions(
                      new PhoneNumber(phoneNumber));
                    messageOptions.From = new PhoneNumber(fromPhoneNumber);
                    messageOptions.Body = $"Your OTP code for BROD registration is {otp}.";
                    var message = MessageResource.Create(messageOptions);

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

                    // Store OTP in the database with expiration time
                    var otpEmail = new OTPEMAIL
                    {
                        email = email,
                        OTP = otp,
                        expirationMin = DateTime.Now.AddMinutes(5)
                    };

                    _context.OtpEmail.InsertOne(otpEmail);

                    // SendGrid API Key from configuration
                    var sendGridApiKey = _configuration["SendGrid:ApiKey"];
                    var fromEmailAddress = _configuration["SendGrid:FromEmailAddress"];

                    // Initialize SendGrid client
                    var client = new SendGrid.SendGridClient(sendGridApiKey);

                    // Prepare email message
                    var from = new SendGrid.Helpers.Mail.EmailAddress(fromEmailAddress, "Brod.com.au Support");
                    var to = new SendGrid.Helpers.Mail.EmailAddress(email);
                    var subject = "Important: Your OTP Code";
                    var plainTextContent = $@"
                Dear User,

                Your One-Time Password (OTP) code is: <strong>{otp}

                Please use this code to complete your verification process. If you did not request this code, please contact our support team immediately.

                Best regards,
                BROD Team";
                    var htmlContent = $@"
                <html>
                    <body>
                        <p>Dear User,</p>
                        <br><p>Your One-Time Password (OTP) code is: <strong>{otp}</strong></p>
                        <p>Please use this code to complete your verification process. If you did not request this code, please contact our support team immediately.</p>
                        <br><p>Best regards,</p>
                        <p><em>BROD Team</em></p>
                    </body>
                </html>";

                    var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                    // Send email
                    var response = await client.SendEmailAsync(msg);
                    var responseBody = await response.Body.ReadAsStringAsync();

                    // Check response
                    if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                    {
                        return Ok("OTP sent successfully.");
                    }
                    else
                    {
                        return BadRequest($"Failed to send OTP. Response code: {response.StatusCode}");
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
                .Set(u => u.suspensionStartDate, null)
                .Set(u => u.Status, "Approved");

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

            [HttpPost("check-email")]
            public async Task<IActionResult> CheckEmail([FromBody] string email)
            {
                var existingUser = await _context.User.Find(u => u.Email == email).FirstOrDefaultAsync();
                if (existingUser != null)
                {
                    return BadRequest("Email already used!");
                }
                else
                {
                    return Ok("Email not used.");
                }

                
            }

            [HttpPut("user/deactivate")]
            public async Task<IActionResult> DeactivateUser(string userId)
            {
                var user = await _context.User.Find(user => user._id == userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound();
                }

                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.Status, "Deactivated");
                await _context.User.UpdateOneAsync(user => user._id == userId, updateDefinition);

                // Get all admins
                var admins = await _context.User.Find(user => user.Role == "Admin").ToListAsync();

                // SendGrid API Key from configuration
                var sendGridApiKey = _configuration["SendGrid:ApiKey"];
                var fromEmailAddress = _configuration["SendGrid:FromEmailAddress"];

                // Initialize SendGrid client
                var client = new SendGrid.SendGridClient(sendGridApiKey);

                foreach (var admin in admins)
                {
                    // Prepare email message
                    var from = new SendGrid.Helpers.Mail.EmailAddress(fromEmailAddress, "Brod System");
                    var to = new SendGrid.Helpers.Mail.EmailAddress(admin.Email);
                    var subject = "User Account Deactivation Request";

                    var plainTextContent = $@"
                User Account Deactivation Request

                Dear Admin,

                The following user has requested to delete their account and delete all associated data:

                - User Email: {user.Email}
                - Request Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} (UTC)

                Please take the necessary actions to process this request promptly.

                Best regards,
                BROD Team";

                    var htmlContent = $@"
                <html>
                    <body style='font-family: Arial, sans-serif; color: #000000;'>
                        <h2 style='color: #000000;'>User Account Deactivation Request</h2>
                        <p>Dear Admin,</p>
                        <br><p>The following user has requested to delete their account and delete all associated data:</p>
                        <ul style='color: #000000;'>
                            <li><strong>User Email:</strong> {user.Email}</li>
                            <li><strong>Request Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} (UTC)</li>
                        </ul>
                        <p>Please take the necessary actions to process this request promptly.</p>
                        <br><p>Best regards,</p>
                        <p><strong>BROD Team</strong></p>
                    </body>
                </html>";

                    var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                    // Send the email
                    var response = await client.SendEmailAsync(msg);

                    if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                    {
                        return BadRequest($"Failed to send notification email to admin: {admin.Email}");
                    }
                }

                return Ok(new { message = "User deactivated successfully" });
            }


            [HttpPut("user/delete")]
            public async Task<IActionResult> DeleteUser(string userId)
            {
                var user = await _context.User.Find(user => user._id == userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound();
                }

                if (user.Role == "Tradie") {
                    var jobs = await _context.Jobs.Find(job => job.TradieID == user._id).ToListAsync();
                    if (jobs != null)
                    {
                        foreach (var x in jobs)
                        {
                            await _context.Jobs.DeleteOneAsync(job => job._id == x._id);
                        }
                    }

                    var services = await _context.Services.Find(ser => ser.UserID == user._id).ToListAsync();
                    if (services != null)
                    {
                        foreach (var x in services)
                        {
                            await _context.Services.DeleteOneAsync(ser => ser._id == x._id);
                        }
                    }

                    var clientMessages = await _context.ClientMessage.Find(ser => ser.TradieId == user._id).ToListAsync();
                    if (clientMessages != null)
                    {
                        foreach (var x in clientMessages)
                        {
                            await _context.ClientMessage.DeleteOneAsync(ser => ser._id == x._id);
                        }
                    }

                    var tradieMessages = await _context.TradieMessage.Find(ser => ser.TradieId == user._id).ToListAsync();
                    if (tradieMessages != null)
                    {
                        foreach (var x in tradieMessages)
                        {
                            await _context.TradieMessage.DeleteOneAsync(ser => ser._id == x._id);
                        }
                    }

                    var notifs = await _context.Notification.Find(ser => ser.userID == user._id).ToListAsync();
                    if (notifs != null)
                    {
                        foreach (var x in notifs)
                        {
                            await _context.Notification.DeleteOneAsync(ser => ser._id == x._id);
                        }
                    }
                }

                if (user.Role == "Client")
                {
                    var jobs = await _context.Jobs.Find(job => job.ClientID == user._id).ToListAsync();
                    if (jobs != null)
                    {
                        foreach (var x in jobs)
                        {
                            await _context.Jobs.DeleteOneAsync(job => job._id == x._id);
                        }
                    }

                    var clientMessages = await _context.ClientMessage.Find(ser => ser.ClientId == user._id).ToListAsync();
                    if (clientMessages != null)
                    {
                        foreach (var x in clientMessages)
                        {
                            await _context.ClientMessage.DeleteOneAsync(ser => ser._id == x._id);
                        }
                    }

                    var tradieMessages = await _context.TradieMessage.Find(ser => ser.ClientId == user._id).ToListAsync();
                    if (tradieMessages != null)
                    {
                        foreach (var x in tradieMessages)
                        {
                            await _context.TradieMessage.DeleteOneAsync(ser => ser._id == x._id);
                        }
                    }

                    var notifs = await _context.Notification.Find(ser => ser.userID == user._id).ToListAsync();
                    if (notifs != null)
                    {
                        foreach (var x in notifs)
                        {
                            await _context.Notification.DeleteOneAsync(ser => ser._id == x._id);
                        }
                    }
                }

                // Delete the user
                await _context.User.DeleteOneAsync(user => user._id == userId);

                // SendGrid API Key from configuration
                var sendGridApiKey = _configuration["SendGrid:ApiKey"];
                var fromEmailAddress = _configuration["SendGrid:FromEmailAddress"];

                // Initialize SendGrid client
                var client = new SendGrid.SendGridClient(sendGridApiKey);

                // Prepare email message
                var from = new SendGrid.Helpers.Mail.EmailAddress(fromEmailAddress, "Brod System");
                var to = new SendGrid.Helpers.Mail.EmailAddress(user.Email);
                var subject = "User Account Deactivation";

                var plainTextContent = $@"
                    Dear User,

                    Your account is already deactivated. For more concerns you can contact us at {fromEmailAddress}

                    Best regards,
                    BROD Team";

                var htmlContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif; color: #000000;'>
                            <p>Dear Admin,</p>
                            <br><p>Your account is already deactivated. For more concerns you can contact us at {fromEmailAddress}</p>
                            <br><p>Best regards,</p>
                            <p><strong>BROD Team</strong></p>
                        </body>
                    </html>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                
                

                return Ok(new { message = "User deleted successfully" });
            }

            [HttpPut("tradie/ScrubData")]
            public async Task<IActionResult> ScrubData()
            {
                var jobs = await _context.Jobs.Find(job => job.ClientName == "").ToListAsync();
                if (jobs != null)
                {
                    foreach (var x in jobs)
                    {
                        await _context.Jobs.DeleteOneAsync(job => job._id == x._id);
                    }
                }

                var jobs2 = await _context.Jobs.Find(job => job.ClientName == "").ToListAsync();
                if (jobs2 != null)
                {
                    foreach (var x in jobs2)
                    {
                        await _context.Jobs.DeleteOneAsync(job2 => job2._id == x._id);
                    }
                }

                var services = await _context.Services.Find(ser => ser.UserID == "").ToListAsync();
                if (services != null)
                {
                    foreach (var x in services)
                    {
                        await _context.Services.DeleteOneAsync(ser => ser._id == x._id);
                    }
                }

                var clientMessages = await _context.ClientMessage.Find(ser => ser.TradieName == "").ToListAsync();
                if (clientMessages != null)
                {
                    foreach (var x in clientMessages)
                    {
                        await _context.ClientMessage.DeleteOneAsync(ser => ser._id == x._id);
                    }
                }

                var tradieMessages = await _context.TradieMessage.Find(ser => ser.TradieId == "").ToListAsync();
                if (tradieMessages != null)
                {
                    foreach (var x in tradieMessages)
                    {
                        await _context.TradieMessage.DeleteOneAsync(ser => ser._id == x._id);
                    }
                }

                var notifs = await _context.Notification.Find(ser => ser.userID == "").ToListAsync();
                if (notifs != null)
                {
                    foreach (var x in notifs)
                    {
                        await _context.Notification.DeleteOneAsync(ser => ser._id == x._id);
                    }
                }

            return Ok(new { message = "User connections deleted successfully" });
            }

            //[HttpPut("DeleteById")]
            //public async Task<IActionResult> DeleteById()
            //{
            //    var jobs = await _context.Jobs.Find(job => job.TradieName == "").ToListAsync();
            //    if (jobs != null)
            //    {
            //        foreach (var x in jobs)
            //        {
            //            await _context.Jobs.DeleteOneAsync(job => job._id == x._id);
            //        }
            //    }

            //    var services = await _context.Services.Find(ser => ser.UserID == "67347d2c7468915d82d71b23").ToListAsync();
            //    if (services != null)
            //    {
            //        foreach (var x in services)
            //        {
            //            await _context.Services.DeleteOneAsync(ser => ser._id == x._id);
            //        }
            //    }

            //    var clientMessages = await _context.ClientMessage.Find(ser => ser.TradieId == "67347d2c7468915d82d71b23").ToListAsync();
            //    if (clientMessages != null)
            //    {
            //        foreach (var x in clientMessages)
            //        {
            //            await _context.ClientMessage.DeleteOneAsync(ser => ser._id == x._id);
            //        }
            //    }

            //    var tradieMessages = await _context.TradieMessage.Find(ser => ser.TradieId == "67347d2c7468915d82d71b23").ToListAsync();
            //    if (tradieMessages != null)
            //    {
            //        foreach (var x in tradieMessages)
            //        {
            //            await _context.TradieMessage.DeleteOneAsync(ser => ser._id == x._id);
            //        }
            //    }

            //    var notifs = await _context.Notification.Find(ser => ser.userID == "67347d2c7468915d82d71b23").ToListAsync();
            //    if (notifs != null)
            //    {
            //        foreach (var x in notifs)
            //        {
            //            await _context.Notification.DeleteOneAsync(ser => ser._id == x._id);
            //        }
            //    }

            //    var reviews = await _context.Rating.Find(ser => ser.tradieId == "67347d2c7468915d82d71b23").ToListAsync();
            //    if (reviews != null)
            //    {
            //        foreach (var x in reviews)
            //        {
            //            await _context.Notification.DeleteOneAsync(ser => ser._id == x._id);
            //        }
            //    }

            //return Ok(new { message = "User connections deleted successfully" });
            //}

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
