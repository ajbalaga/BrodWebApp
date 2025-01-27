using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Configuration;

namespace BrodClientAPI.Service
{
    public class TwilioService
    {
        private readonly IConfiguration _configuration;

        public TwilioService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendSmsAsync(string toPhoneNumber, string otp)
        {
            // Retrieve Twilio credentials from appsettings
            var accountSid = _configuration["Twilio:AccountSID"];
            var authToken = _configuration["Twilio:AuthToken"];
            var fromPhoneNumber = _configuration["Twilio:FromPhoneNumber"];

            // Initialize Twilio client
            TwilioClient.Init(accountSid, authToken);

            // Send SMS
            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber(toPhoneNumber),
                from: new PhoneNumber(fromPhoneNumber),
                body: $"Your OTP code is {otp}"
            );
        }
    }
}
