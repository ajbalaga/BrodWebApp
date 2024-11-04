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
                
        [HttpPost("AddReviewToJobPost")]
        public IActionResult AddReviewToJobPost([FromBody] AddReviewToJobPostAd reviewDetails)
        {
            try
            {
                var client = _context.User.Find(user => user._id == reviewDetails.ClientID && user.Role == "Client").FirstOrDefault();
                if (client == null)
                {
                    return NotFound();
                }

                var existingService = _context.Services.Find(service => service._id == reviewDetails.ServiceID).FirstOrDefault();
                if (existingService == null)
                {   
                    return NotFound();
                }

                var review = new Reviews { 
                _id = "",
                ServiceID = reviewDetails.ServiceID,
                ClientID = reviewDetails.ClientID,
                ClientUserName = client.Username,
                ClientCity = client.City,
                ClientState = client.State,
                ClientPostalCode = client.PostalCode,
                StarRating = reviewDetails.StarRating,
                ReviewDescription = reviewDetails.ReviewDescription                             
                };
                var updateDefinitions = new List<UpdateDefinition<Services>>();
                _context.Reviews.InsertOne(review);

                // Prepare the update to append the review to the ClientReviews list in the Service
                var update = Builders<Services>.Update.Push(s => s.ClientReviews, new Review
                {
                    ReviewDescription = reviewDetails.ReviewDescription,
                    StarRating = reviewDetails.StarRating,
                    ClientID = reviewDetails.ClientID,
                    ClientUserName = client.Username,
                    ClientCity = client.City,
                    ClientState = client.State,
                    ClientPostalCode = client.PostalCode
                });

                // Update the service with the new review
                _context.Services.UpdateOne(service => service._id == reviewDetails.ServiceID, update);


                return Ok(new { message = "Review post added successfully" });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding review to job post", error = ex.Message });
            }
        }

        [HttpPost("HireTradie")]
        public IActionResult HireTradie([FromBody] HireTradie hireTradieDetails)
        {
            try
            {
                var client = _context.User.Find(user => user._id == hireTradieDetails.ClientID && user.Role == "Client").FirstOrDefault();
                if (client == null)
                {
                    return NotFound();
                }

                var existingService = _context.Services.Find(service => service._id == hireTradieDetails.ServiceID).FirstOrDefault();
                if (existingService == null)
                {
                    return NotFound();
                }
                var tradieDetails = _context.User.Find(user => user._id == existingService.UserID && user.Role == "Tradie").FirstOrDefault();

                var jobDetails = new Jobs
                {
                    _id = "",
                    ServiceID = hireTradieDetails.ServiceID,
                    ClientID = hireTradieDetails.ClientID,
                    TradieID = existingService.UserID,
                    Status= "Pending",
                    DescriptionServiceNeeded= hireTradieDetails.DescriptionServiceNeeded,
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
                var updateDefinitions = new List<UpdateDefinition<Jobs>>();
                _context.Jobs.InsertOne(jobDetails);

                var addCountJobOffer = new UpdateCount { TradieID = existingService.UserID, Count = tradieDetails.PendingOffers + 1 };
                UpdateJobOfferCount(addCountJobOffer);

                return Ok(new { message = "Job offer submitted successfully" });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while sending job offer", error = ex.Message });
            }
        }
        [HttpPost("BookmarkJob")]
        public IActionResult BookmarkJob([FromBody] HireTradie hireTradieDetails)
        {
            try
            {
                var client = _context.User.Find(user => user._id == hireTradieDetails.ClientID && user.Role == "Client").FirstOrDefault();
                if (client == null)
                {
                    return NotFound();
                }

                var existingService = _context.Services.Find(service => service._id == hireTradieDetails.ServiceID).FirstOrDefault();
                if (existingService == null)
                {
                    return NotFound();
                }

                var tradieDetails = _context.User.Find(user => user._id == existingService.UserID && user.Role == "Tradie").FirstOrDefault();

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
                var updateDefinitions = new List<UpdateDefinition<Jobs>>();
                _context.Jobs.InsertOne(jobDetails);

                var tradie = _context.User.Find(user => user._id == existingService.UserID && user.Role.ToLower() == "tradie").FirstOrDefault();
                var addCountJobOffer = new UpdateCount { TradieID = existingService.UserID, Count = tradie.PendingOffers + 1 };
                UpdateJobOfferCount(addCountJobOffer);

                return Ok(new { message = "Job offer submitted successfully" });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while sending job offer", error = ex.Message });
            }
        }        

        [HttpPost("GetJobsByStatus")]
        public IActionResult GetFilteredJobs([FromBody] GetJobsByStatus jobsByStatus)
        {
            try
            {
                // Step 2: Filter jobs based on Status and ClientID
                var jobFilterBuilder = Builders<Jobs>.Filter;
                var jobFilter = jobFilterBuilder.Eq(job => job.Status, jobsByStatus.Status) &
                                jobFilterBuilder.Eq(job => job.ClientID, jobsByStatus.UserID);

                var jobs = _context.Jobs.Find(jobFilter).ToListAsync().Result;
                var updatedJobs = new List<Jobs> { };
                foreach (var job in jobs) { 
                    var tradie = job.TradieID;
                    var tradieDetails = _context.User.Find(user => user._id == tradie && user.Role == "Tradie").FirstOrDefault();
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
        public IActionResult UpdateJobStatus([FromBody] UpdateJobStatus updateJobStatus)
        {   
            try
            {
                var job = _context.Jobs.Find(job => job._id == updateJobStatus.JobID).FirstOrDefault();
                if (job == null)
                {
                    return NotFound();
                }

                var updateDefinitions = new List<UpdateDefinition<Jobs>>();

                // Update fields only if they are provided in updateJobStatus
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

                _context.Jobs.UpdateOne(filter, updateDefinition);

                var tradie = _context.User.Find(user => user._id == updateJobStatus.TradieID && user.Role.ToLower() == "tradie").FirstOrDefault();

                if (updateJobStatus.Status.ToLower() == "cancelled") 
                {
                    var jobCount = tradie.PendingOffers == 0 ? 0 : tradie.PendingOffers - 1;
                    var countVal = new UpdateCount { TradieID = updateJobStatus.TradieID, Count = jobCount };
                        UpdateJobOfferCount(countVal);
                } // -1 for pending offer count for tradie

                if (updateJobStatus.Status.ToLower() == "completed") 
                {
                    var addCountJobCompleted = new UpdateCount { TradieID = updateJobStatus.TradieID, Count = tradie.CompletedJobs + 1 };
                    UpdateCompletetedJobs(addCountJobCompleted);

                    var earningAmount = Convert.ToDecimal(tradie.EstimatedEarnings) + Convert.ToDecimal(job.ClientBudget);
                    var addEarning= new UpdateEstimatedEarning { TradieID = updateJobStatus.TradieID, Earning = earningAmount };
                    UpdateEstimatedEarningOfTradie(addEarning);
                } // from in progress to completed (+ count for completed job) AND update estimated earning
                

                return Ok(new { message = "Job status updated successfully" });               
                

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the profile", error = ex.Message });
            }
        }

        // job offer count
        private IActionResult UpdateJobOfferCount([FromBody] UpdateCount updateCount)
        {
            try
            {
                var tradie = _context.User.Find(user => user._id == updateCount.TradieID && user.Role == "Tradie").FirstOrDefault();
                if (tradie == null)
                {
                    return NotFound();
                }

                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.PendingOffers, updateCount.Count);
                _context.User.UpdateOne(user => user._id == updateCount.TradieID, updateDefinition);

                return Ok(new { message = "Job offer count updated: " + updateCount.Count.ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating Job offer count", error = ex.Message });
            }
        }

        // completed jobs
        private IActionResult UpdateCompletetedJobs([FromBody] UpdateCount updateCount)
        {
            try
            {
                var tradie = _context.User.Find(user => user._id == updateCount.TradieID && user.Role == "Tradie").FirstOrDefault();
                if (tradie == null)
                {
                    return NotFound();
                }

                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.CompletedJobs, updateCount.Count);
                _context.User.UpdateOne(user => user._id == updateCount.TradieID, updateDefinition);

                return Ok(new { message = "Completed jobs count updated: " + updateCount.Count.ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating completed job count", error = ex.Message });
            }
        }

        // estimated earning
        private IActionResult UpdateEstimatedEarningOfTradie([FromBody] UpdateEstimatedEarning updateEarning)
        {
            try
            {
                var tradie = _context.User.Find(user => user._id == updateEarning.TradieID && user.Role == "Tradie").FirstOrDefault();
                if (tradie == null)
                {
                    return NotFound();
                }

                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.EstimatedEarnings, updateEarning.Earning);
                _context.User.UpdateOne(user => user._id == updateEarning.TradieID, updateDefinition);

                return Ok(new { message = "Earning updated: " + updateEarning.Earning.ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating earning", error = ex.Message });
            }
        }

        //[HttpGet("myDetails")]
        //public IActionResult GetClientById([FromBody] OwnProfile getTradieProfile)
        //{
        //    try
        //    {
        //        var client = _context.User.Find(user => user._id == getTradieProfile.ID && user.Role == "Client").FirstOrDefault();
        //        if (client == null)
        //        {
        //            return NotFound(new { message = "Client not found" });
        //        }
        //        return Ok(client);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred while getting Client details", error = ex.Message });
        //    }

        //}


        //[HttpGet("allServices")]
        //public IActionResult GetAllServices()
        //{
        //    try
        //    {
        //        var services = _context.Services.Find(services => true).ToList(); // Fetch all users from MongoDB
        //        return Ok(services);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred while retrieving services", error = ex.Message });
        //    }
        //}

        //[HttpGet("FilteredServices")]
        //public IActionResult GetFilteredServices([FromBody] JobAdPostFilter filterInput)
        //{
        //    try
        //    {
        //        var filterBuilder = Builders<Services>.Filter;
        //        var filter = filterBuilder.Empty; // Start with an empty filter

        //        // Filter by Postcode
        //        if (!string.IsNullOrEmpty(filterInput.Postcode))
        //        {
        //            filter &= filterBuilder.Eq(s => s.BusinessPostcode, filterInput.Postcode);
        //        }

        //        // Filter by JobCategory (if multiple categories are provided)
        //        if (filterInput.JobCategories != null && filterInput.JobCategories.Count > 0)
        //        {
        //            filter &= filterBuilder.In(s => s.JobCategory, filterInput.JobCategories);
        //        }

        //        // Filter by Keywords (match JobAdTitle using a case-insensitive regex)
        //        if (!string.IsNullOrEmpty(filterInput.Keywords))
        //        {
        //            var regexFilter = new BsonRegularExpression(filterInput.Keywords, "i"); // Case-insensitive search
        //            filter &= filterBuilder.Regex(s => s.JobAdTitle, regexFilter);
        //        }


        //        // Filter by PricingStartsAt (range between min and max)
        //        if (filterInput.PricingStartsMax> filterInput.PricingStartsMin)
        //        {
        //            filter &= filterBuilder.Eq(s => s.PricingOption, "Hourly");
        //            filter &= filterBuilder.Gte(s => s.PricingStartsAt, filterInput.PricingStartsMin.ToString()) &
        //                      filterBuilder.Lte(s => s.PricingStartsAt, filterInput.PricingStartsMax.ToString());
        //        }

        //        var filteredServices = _context.Services.Find(filter).ToList();

        //        var userIds = filteredServices.Select(s => s.UserID).Distinct().ToList();


        //        var userFilterBuilder = Builders<User>.Filter;
        //        var userFilter = userFilterBuilder.In(u => u._id, userIds);

        //        if (filterInput.CallOutRateMax > filterInput.CallOutRateMin)
        //        {
        //            userFilter &= userFilterBuilder.Gte(u => u.CallOutRate, filterInput.CallOutRateMin.Value.ToString()) &
        //                          userFilterBuilder.Lte(u => u.CallOutRate, filterInput.CallOutRateMin.Value.ToString());
        //        }

        //        // Filter by ProximityToWork (min and max range)
        //        if (filterInput.ProximityToWorkMax > filterInput.ProximityToWorkMin)
        //        {
        //            userFilter &= userFilterBuilder.Gte(u => u.ProximityToWork, filterInput.ProximityToWorkMin.Value.ToString()) &
        //                          userFilterBuilder.Lte(u => u.ProximityToWork, filterInput.ProximityToWorkMax.Value.ToString());
        //        }

        //        // Filter by AvailabilityToWork (multiple answers)
        //        if (!String.IsNullOrEmpty(filterInput.AvailabilityToWork[0]) && filterInput.AvailabilityToWork.Count > 0)
        //        {
        //            userFilter &= userFilterBuilder.In(u => u.AvailabilityToWork, filterInput.AvailabilityToWork);
        //        }

        //        // Fetch the filtered users that match the user filters
        //        var filteredUsers = _context.User.Find(userFilter).ToList();
        //        var finalUserIds = filteredUsers.Select(u => u._id).ToList();

        //        // Step 4: Filter the services again based on the final list of UserIDs from the User filter
        //        var finalServices = filteredServices.Where(s => finalUserIds.Contains(s.UserID)).ToList();

        //        return Ok(finalServices);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred while retrieving services", error = ex.Message });
        //    }
        //}

        //[HttpGet("JobPostDetails")]
        //public IActionResult GetJobPostDetails([FromBody] OwnProfile serviceProfile)
        //{
        //    try
        //    {
        //        var service = _context.Services.Find(service => service._id == serviceProfile.ID).FirstOrDefault();
        //        if (service == null)
        //        {
        //            return NotFound(new { message = "Job Ad Post not found" });
        //        }
        //        return Ok(service);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred while getting job post", error = ex.Message });
        //    }
        //}
    }
}
