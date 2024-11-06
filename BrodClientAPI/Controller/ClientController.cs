using BrodClientAPI.Data;
using BrodClientAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;

namespace BrodClientAPI.Controller
{
    [Authorize(Policy = "ClientPolicy")]
    [ApiController]
    [Route("api/[controller]")]
    public class ClientController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public ClientController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpPut("update-profile")]
        public IActionResult UpdateProfile([FromBody] User clientProfile)
        {
            try
            {
                var client = _context.User.Find(user => user._id == clientProfile._id && user.Role == "Client").FirstOrDefault();
                if (client == null)
                {
                    return NotFound();
                }

                var updateDefinitions = new List<UpdateDefinition<User>>();

                // Update fields only if they are provided in clientProfile
                if (!string.IsNullOrEmpty(clientProfile.Username) && client.Username != clientProfile.Username)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.Username, clientProfile.Username));
                }
                if (!string.IsNullOrEmpty(clientProfile.FirstName) && client.FirstName != clientProfile.FirstName)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.FirstName, clientProfile.FirstName));
                }
                if (!string.IsNullOrEmpty(clientProfile.LastName) && client.LastName != clientProfile.LastName)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.LastName, clientProfile.LastName));
                }
                if (!string.IsNullOrEmpty(clientProfile.Username) && client.Username != clientProfile.Username)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.Username, clientProfile.Username));
                }
                if (!string.IsNullOrEmpty(clientProfile.Email) && client.Email != clientProfile.Email)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.Email, clientProfile.Email));
                }
                if (!string.IsNullOrEmpty(clientProfile.ContactNumber) && client.ContactNumber != clientProfile.ContactNumber)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.ContactNumber, clientProfile.ContactNumber));
                }
                if (!string.IsNullOrEmpty(clientProfile.State) && client.State != clientProfile.State)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.State, clientProfile.State));
                }
                if (!string.IsNullOrEmpty(clientProfile.City) && client.City != clientProfile.City)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.City, clientProfile.City));
                }
                if (!string.IsNullOrEmpty(clientProfile.PostalCode) && client.PostalCode != clientProfile.PostalCode)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.PostalCode, clientProfile.PostalCode));
                }


                if (client.ProfilePicture != clientProfile.ProfilePicture)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.ProfilePicture, clientProfile.ProfilePicture ?? string.Empty));
                }

                if (updateDefinitions.Count == 0)
                {
                    return BadRequest(new { message = "No valid fields to update" });
                }

                var updateDefinition = Builders<User>.Update.Combine(updateDefinitions);
                var filter = Builders<User>.Filter.Eq(u => u._id, clientProfile._id);

                _context.User.UpdateOne(filter, updateDefinition);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the profile", error = ex.Message });
            }

            return Ok(new { message = "Client profile updated successfully" });
        }

        [HttpPost("HireTradie")]
        public async Task<IActionResult> HireTradie([FromBody] HireTradie hireTradieDetails)
        {
            try
            {
                var client = await _context.User.Find(user => user._id == hireTradieDetails.ClientID && user.Role == "Client").FirstOrDefaultAsync();
                if (client == null)
                {
                    return NotFound();
                }

                var existingService = await _context.Services.Find(service => service._id == hireTradieDetails.ServiceID).FirstOrDefaultAsync();
                if (existingService == null)
                {
                    return NotFound();
                }

                var tradieDetails = await _context.User.Find(user => user._id == existingService.UserID && user.Role == "Tradie").FirstOrDefaultAsync();

                var jobDetails = new Jobs
                {
                    _id = "",
                    ServiceID = hireTradieDetails.ServiceID,
                    ClientID = hireTradieDetails.ClientID,
                    TradieID = existingService.UserID,
                    Status = "Pending",
                    DescriptionServiceNeeded = hireTradieDetails.DescriptionServiceNeeded,
                    ClientName = $"{client.FirstName} {client.LastName}",
                    ClientContactNumber = hireTradieDetails.ClientContactNumber,
                    ClientCity = client.City,
                    ClientState = client.State,
                    ClientPostalCode = hireTradieDetails.ClientPostCode,
                    JobPostAdTitle = existingService.JobAdTitle,
                    StartDate = hireTradieDetails.StartDate,
                    CompletionDate = hireTradieDetails.CompletionDate,
                    ClientBudget = hireTradieDetails.ClientBudget,
                    BudgetCurrency = hireTradieDetails.BudgetCurrency,
                    JobAdDescription = existingService.DescriptionOfService,
                    JobActionDate = hireTradieDetails.JobActionDate,
                    TradieLocation = $"{tradieDetails.City},{tradieDetails.State} {tradieDetails.PostalCode}",
                    Proximity = tradieDetails.ProximityToWork
                };

                await _context.Jobs.InsertOneAsync(jobDetails);

                var addCountJobOffer = new UpdateCount { TradieID = existingService.UserID, Count = tradieDetails.PendingOffers + 1 };
                await UpdateJobOfferCount(addCountJobOffer);

                return Ok(new { message = "Job offer submitted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while sending job offer", error = ex.Message });
            }
        }

        [HttpPost("BookmarkJob")]
        public async Task<IActionResult> BookmarkJob([FromBody] HireTradie hireTradieDetails)
        {
            try
            {
                var client = await _context.User.Find(user => user._id == hireTradieDetails.ClientID && user.Role == "Client").FirstOrDefaultAsync();
                if (client == null)
                {
                    return NotFound();
                }

                var existingService = await _context.Services.Find(service => service._id == hireTradieDetails.ServiceID).FirstOrDefaultAsync();
                if (existingService == null)
                {
                    return NotFound();
                }

                var tradieDetails = await _context.User.Find(user => user._id == existingService.UserID && user.Role == "Tradie").FirstOrDefaultAsync();

                var jobDetails = new Jobs
                {
                    _id = "",
                    ServiceID = hireTradieDetails.ServiceID,
                    ClientID = hireTradieDetails.ClientID,
                    TradieID = existingService.UserID,
                    Status = "Bookmarked",
                    DescriptionServiceNeeded = hireTradieDetails.DescriptionServiceNeeded,
                    ClientName = $"{client.FirstName} {client.LastName}",
                    ClientContactNumber = hireTradieDetails.ClientContactNumber,
                    ClientCity = client.City,
                    ClientState = client.State,
                    ClientPostalCode = hireTradieDetails.ClientPostCode,
                    JobPostAdTitle = existingService.JobAdTitle,
                    StartDate = hireTradieDetails.StartDate,
                    CompletionDate = hireTradieDetails.CompletionDate,
                    ClientBudget = hireTradieDetails.ClientBudget,
                    BudgetCurrency = hireTradieDetails.BudgetCurrency,
                    JobAdDescription = existingService.DescriptionOfService,
                    JobActionDate = hireTradieDetails.JobActionDate,
                    TradieLocation = $"{tradieDetails.City},{tradieDetails.State} {tradieDetails.PostalCode}",
                    Proximity = tradieDetails.ProximityToWork
                };

                await _context.Jobs.InsertOneAsync(jobDetails);

                var tradie = await _context.User.Find(user => user._id == existingService.UserID && user.Role.ToLower() == "tradie").FirstOrDefaultAsync();
                var addCountJobOffer = new UpdateCount { TradieID = existingService.UserID, Count = tradie.PendingOffers + 1 };
                await UpdateJobOfferCount(addCountJobOffer);

                return Ok(new { message = "Job offer submitted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while sending job offer", error = ex.Message });
            }
        }

        [HttpPost("GetJobsByStatus")]
        public async Task<IActionResult> GetFilteredJobs([FromBody] GetJobsByStatus jobsByStatus)
        {
            try
            {
                var jobFilterBuilder = Builders<Jobs>.Filter;
                var jobFilter = jobFilterBuilder.Eq(job => job.Status, jobsByStatus.Status) &
                                jobFilterBuilder.Eq(job => job.ClientID, jobsByStatus.UserID);

                var jobs = await _context.Jobs.Find(jobFilter).ToListAsync();
                var updatedJobs = new List<Jobs>();

                foreach (var job in jobs)
                {
                    var tradie = job.TradieID;
                    var tradieDetails = await _context.User.Find(user => user._id == tradie && user.Role == "Tradie").FirstOrDefaultAsync();
                    job.TradieLocation = $"{tradieDetails.City},{tradieDetails.State} {tradieDetails.PostalCode}";
                    job.Proximity = tradieDetails.ProximityToWork;
                    job.TradieName = $"{tradieDetails.FirstName} {tradieDetails.LastName}";
                    updatedJobs.Add(job);
                }

                return Ok(updatedJobs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while getting job post", error = ex.Message });
            }
        }

        [HttpPut("UpdateJobStatus")]
        public async Task<IActionResult> UpdateJobStatus([FromBody] UpdateJobStatus updateJobStatus)
        {
            try
            {
                var job = await _context.Jobs.Find(job => job._id == updateJobStatus.JobID).FirstOrDefaultAsync();
                if (job == null)
                {
                    return NotFound();
                }

                var updateDefinitions = new List<UpdateDefinition<Jobs>>();

                if (updateJobStatus.Status != null && job.Status != updateJobStatus.Status)
                {
                    updateDefinitions.Add(Builders<Jobs>.Update.Set(u => u.Status, updateJobStatus.Status));
                }
                if (updateJobStatus.JobActionDate != null && job.JobActionDate != updateJobStatus.JobActionDate)
                {
                    updateDefinitions.Add(Builders<Jobs>.Update.Set(u => u.JobActionDate, updateJobStatus.JobActionDate));
                }

                if (updateDefinitions.Count == 0)
                {
                    return BadRequest(new { message = "No valid fields to update" });
                }

                var updateDefinition = Builders<Jobs>.Update.Combine(updateDefinitions);
                var filter = Builders<Jobs>.Filter.Eq(u => u._id, updateJobStatus.JobID);

                await _context.Jobs.UpdateOneAsync(filter, updateDefinition);

                var tradie = await _context.User.Find(user => user._id == updateJobStatus.TradieID && user.Role.ToLower() == "tradie").FirstOrDefaultAsync();

                if (updateJobStatus.Status.ToLower() == "cancelled")
                {
                    var jobCount = tradie.PendingOffers == 0 ? 0 : tradie.PendingOffers - 1;
                    var countVal = new UpdateCount { TradieID = updateJobStatus.TradieID, Count = jobCount };
                    await UpdateJobOfferCount(countVal);
                }

                if (updateJobStatus.Status.ToLower() == "completed")
                {
                    var addCountJobCompleted = new UpdateCount { TradieID = updateJobStatus.TradieID, Count = tradie.CompletedJobs + 1 };
                    await UpdateCompletedJobs(addCountJobCompleted);

                    var earningAmount = Convert.ToDecimal(tradie.EstimatedEarnings) + Convert.ToDecimal(job.ClientBudget);
                    var addEarning = new UpdateEstimatedEarning { TradieID = updateJobStatus.TradieID, Earning = earningAmount };
                    await UpdateEstimatedEarningOfTradie(addEarning);
                }

                return Ok(new { message = "Job status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the profile", error = ex.Message });
            }
        }

        [HttpPost("AddRating")]
        public async Task<IActionResult> AddRating([FromBody] Rating rating)
        {
            try
            {
                var client = await _context.User.Find(user => user._id == rating.clientId && user.Role == "Client").FirstOrDefaultAsync();
                if (client == null)
                {
                    return NotFound();
                }
                rating.clientLocation = $"{client.City},{client.State} {client.PostalCode}";

                await _context.Rating.InsertOneAsync(rating);

                return Ok(rating);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding rating", error = ex.Message });
            }
        }

        // job offer count
        private async Task<IActionResult> UpdateJobOfferCount([FromBody] UpdateCount updateCount)
        {
            try
            {
                var tradie = await _context.User.Find(user => user._id == updateCount.TradieID && user.Role == "Tradie").FirstOrDefaultAsync();
                if (tradie == null)
                {
                    return NotFound();
                }

                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.PendingOffers, updateCount.Count);
                await _context.User.UpdateOneAsync(user => user._id == updateCount.TradieID, updateDefinition);

                return Ok(new { message = "Job offer count updated: " + updateCount.Count.ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating Job offer count", error = ex.Message });
            }
        }

        // completed jobs
        private async Task<IActionResult> UpdateCompletedJobs([FromBody] UpdateCount updateCount)
        {
            try
            {
                var tradie = await _context.User.Find(user => user._id == updateCount.TradieID && user.Role == "Tradie").FirstOrDefaultAsync();
                if (tradie == null)
                {
                    return NotFound();
                }

                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.CompletedJobs, updateCount.Count);
                await _context.User.UpdateOneAsync(user => user._id == updateCount.TradieID, updateDefinition);

                return Ok(new { message = "Completed jobs count updated: " + updateCount.Count.ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating completed job count", error = ex.Message });
            }
        }

        // estimated earning
        private async Task<IActionResult> UpdateEstimatedEarningOfTradie([FromBody] UpdateEstimatedEarning updateEarning)
        {
            try
            {
                var tradie = await _context.User.Find(user => user._id == updateEarning.TradieID && user.Role == "Tradie").FirstOrDefaultAsync();
                if (tradie == null)
                {
                    return NotFound();
                }

                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.EstimatedEarnings, updateEarning.Earning);
                await _context.User.UpdateOneAsync(user => user._id == updateEarning.TradieID, updateDefinition);

                return Ok(new { message = "Earning updated: " + updateEarning.Earning.ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating earning", error = ex.Message });
            }
        }


        //[HttpPost("AddReviewToJobPost")]
        //public IActionResult AddReviewToJobPost([FromBody] AddReviewToJobPostAd reviewDetails)
        //{
        //    try
        //    {
        //        var client = _context.User.Find(user => user._id == reviewDetails.ClientID && user.Role == "Client").FirstOrDefault();
        //        if (client == null)
        //        {
        //            return NotFound();
        //        }

        //        var existingService = _context.Services.Find(service => service._id == reviewDetails.ServiceID).FirstOrDefault();
        //        if (existingService == null)
        //        {   
        //            return NotFound();
        //        }

        //        var review = new Reviews { 
        //        _id = "",
        //        ServiceID = reviewDetails.ServiceID,
        //        ClientID = reviewDetails.ClientID,
        //        ClientUserName = client.Username,
        //        ClientCity = client.City,
        //        ClientState = client.State,
        //        ClientPostalCode = client.PostalCode,
        //        StarRating = reviewDetails.StarRating,
        //        ReviewDescription = reviewDetails.ReviewDescription                             
        //        };
        //        var updateDefinitions = new List<UpdateDefinition<Services>>();
        //        _context.Reviews.InsertOne(review);

        //        // Prepare the update to append the review to the ClientReviews list in the Service
        //        var update = Builders<Services>.Update.Push(s => s.ClientReviews, new Review
        //        {
        //            ReviewDescription = reviewDetails.ReviewDescription,
        //            StarRating = reviewDetails.StarRating,
        //            ClientID = reviewDetails.ClientID,
        //            ClientUserName = client.Username,
        //            ClientCity = client.City,
        //            ClientState = client.State,
        //            ClientPostalCode = client.PostalCode
        //        });

        //        // Update the service with the new review
        //        _context.Services.UpdateOne(service => service._id == reviewDetails.ServiceID, update);


        //        return Ok(new { message = "Review post added successfully" });

        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred while adding review to job post", error = ex.Message });
        //    }
        //}
    }
}
