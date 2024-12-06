using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BrodClientAPI.Data;
using BrodClientAPI.Models;
using MongoDB.Driver;

namespace BrodClientAPI.Controller
{
    [Authorize(Policy = "TradiePolicy")]
    [ApiController]
    [Route("api/[controller]")]
    public class TradieController : ControllerBase
    {
        private readonly ApiDbContext _context;
        public TradieController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpPut("update-tradie-profile")]
        public async Task<IActionResult> UpdateTradieProfile([FromBody] UpdateUserProfile tradieProfile)
        {
            try
            {
                var tradie = await _context.User.Find(user => user._id == tradieProfile.ID && user.Role == "Tradie").FirstOrDefaultAsync();
                if (tradie == null)
                {
                    return NotFound(new object[0]);
                }

                var updateDefinitions = new List<UpdateDefinition<User>>();

                // Update fields only if they are provided in tradieProfile
                if (!string.IsNullOrEmpty(tradieProfile.FirstName) && tradieProfile.FirstName != tradie.FirstName)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.FirstName, tradieProfile.FirstName));
                }
                if (!string.IsNullOrEmpty(tradieProfile.LastName) && tradieProfile.LastName != tradie.LastName)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.LastName, tradieProfile.LastName));
                }
                if (!string.IsNullOrEmpty(tradieProfile.RegisteredBusinessName) && tradieProfile.RegisteredBusinessName != tradie.RegisteredBusinessName)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.RegisteredBusinessName, tradieProfile.RegisteredBusinessName));
                }
                if (!string.IsNullOrEmpty(tradieProfile.BusinessAddress) && tradieProfile.BusinessAddress != tradie.BusinessAddress)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.BusinessAddress, tradieProfile.BusinessAddress));
                }
                if (!string.IsNullOrEmpty(tradieProfile.BusinessPostCode) && tradieProfile.BusinessPostCode != tradie.BusinessPostCode)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.BusinessPostCode, tradieProfile.BusinessPostCode));
                }
                if (!string.IsNullOrEmpty(tradieProfile.TypeofWork) && tradieProfile.TypeofWork != tradie.TypeofWork)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.TypeofWork, tradieProfile.TypeofWork));
                }
                if (!string.IsNullOrEmpty(tradieProfile.AustralianBusinessNumber) && tradieProfile.AustralianBusinessNumber != tradie.AustralianBusinessNumber)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.AustralianBusinessNumber, tradieProfile.AustralianBusinessNumber));
                }
                if (!string.IsNullOrEmpty(tradieProfile.ContactNumber) && tradieProfile.ContactNumber != tradie.ContactNumber)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.ContactNumber, tradieProfile.ContactNumber));
                }
                if (!string.IsNullOrEmpty(tradieProfile.AvailabilityToWork) && tradieProfile.AvailabilityToWork != tradie.AvailabilityToWork)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.AvailabilityToWork, tradieProfile.AvailabilityToWork));
                }
                if (!string.IsNullOrEmpty(tradieProfile.CallOutRate) && tradieProfile.CallOutRate != tradie.CallOutRate)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.CallOutRate, tradieProfile.CallOutRate));
                }
                if (!string.IsNullOrEmpty(tradieProfile.Email) && tradieProfile.Email != tradie.Email)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.Email, tradieProfile.Email));
                }
                if (!string.IsNullOrEmpty(tradieProfile.Website) && tradieProfile.Website != tradie.Website)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.Website, tradieProfile.Website));
                }
                if (!string.IsNullOrEmpty(tradieProfile.FacebookAccount) && tradieProfile.FacebookAccount != tradie.FacebookAccount)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.FacebookAccount, tradieProfile.FacebookAccount));
                }
                if (!string.IsNullOrEmpty(tradieProfile.IGAccount) && tradieProfile.IGAccount != tradie.IGAccount)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.IGAccount, tradieProfile.IGAccount));
                }
                if (!string.IsNullOrEmpty(tradieProfile.AboutMeDescription) && tradieProfile.AboutMeDescription != tradie.AboutMeDescription)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.AboutMeDescription, tradieProfile.AboutMeDescription));
                }
                if (!string.IsNullOrEmpty(tradieProfile.ProximityToWork) && tradieProfile.ProximityToWork != tradie.ProximityToWork)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.ProximityToWork, tradieProfile.ProximityToWork));
                }
                if (!string.IsNullOrEmpty(tradieProfile.City) && tradieProfile.City != tradie.City)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.City, tradieProfile.City));
                }
                if (!string.IsNullOrEmpty(tradieProfile.State) && tradieProfile.State != tradie.State)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.State, tradieProfile.State));
                }
                if (!string.IsNullOrEmpty(tradieProfile.PostalCode) && tradieProfile.PostalCode != tradie.PostalCode)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.PostalCode, tradieProfile.PostalCode));
                }
                if (!string.IsNullOrEmpty(tradieProfile.ContactNumber) && tradieProfile.ContactNumber != tradieProfile.ContactNumber)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.ContactNumber, tradieProfile.ContactNumber));
                }
                if (tradieProfile.ProfilePicture != tradie.ProfilePicture)
                {
                    updateDefinitions.Add(Builders<User>.Update.Set(u => u.ProfilePicture, tradieProfile.ProfilePicture ?? string.Empty));
                }
                if (tradieProfile.CertificationFilesUploaded != null)
                {
                    // Ensure tradie.Services is initialized (default to empty list if null)
                    var currentCertifications = tradie.CertificationFilesUploaded ?? new List<string>();

                    // Compare lists considering possible null values
                    if (!tradieProfile.CertificationFilesUploaded.SequenceEqual(currentCertifications))
                    {
                        updateDefinitions.Add(Builders<User>.Update.Set(u => u.CertificationFilesUploaded, tradieProfile.CertificationFilesUploaded));
                    }
                }
                if (tradieProfile.Services != null)
                {
                    // Ensure tradie.Services is initialized (default to empty list if null)
                    var currentServices = tradie.Services ?? new List<string>();

                    // Compare lists considering possible null values
                    if (!tradieProfile.Services.SequenceEqual(currentServices))
                    {
                        updateDefinitions.Add(Builders<User>.Update.Set(u => u.Services, tradieProfile.Services));
                    }
                }
                updateDefinitions.Add(Builders<User>.Update.Set(u => u.LastActivityTimeStamp, DateTime.Now));
                updateDefinitions.Add(Builders<User>.Update.Set(u => u.LastActivity, "Update Profile Details"));

                if (updateDefinitions.Count == 0)
                {
                    return BadRequest(new { message = "No valid fields to update" });
                }

                var updateDefinition = Builders<User>.Update.Combine(updateDefinitions);
                var filter = Builders<User>.Filter.Eq(u => u._id, tradieProfile.ID);

                await _context.User.UpdateOneAsync(filter, updateDefinition);

                return Ok(new { message = "Tradie profile updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while getting tradie profile details", error = ex.Message });
            }
        }

        [HttpPut("update-tradie-profile-picture")]
        public async Task<IActionResult> UpdateTradieProfilePicture([FromBody] UpdateTradieProfilePicture tradieProfile)
        {
            try
            {
                var tradie = await _context.User.Find(user => user._id == tradieProfile.ID && user.Role == "Tradie").FirstOrDefaultAsync();
                if (tradie == null)
                {
                    return NotFound(new object[0]);
                }

                var updateDefinitions = new List<UpdateDefinition<User>>();

                if (tradieProfile.ProfilePicture != null && tradieProfile.ProfilePicture.Length > 0)
                {
                    if (!string.IsNullOrEmpty(tradie.ProfilePicture))
                    {
                        updateDefinitions.Add(Builders<User>.Update.Set(u => u.ProfilePicture, tradieProfile.ProfilePicture));
                    }
                }
                updateDefinitions.Add(Builders<User>.Update.Set(u => u.LastActivityTimeStamp, DateTime.Now));
                updateDefinitions.Add(Builders<User>.Update.Set(u => u.LastActivity, "Update Profile Picture"));

                if (updateDefinitions.Count == 0)
                {
                    return BadRequest(new { message = "No valid fields to update" });
                }

                var updateDefinition = Builders<User>.Update.Combine(updateDefinitions);
                var filter = Builders<User>.Filter.Eq(u => u._id, tradieProfile.ID);

                await _context.User.UpdateOneAsync(filter, updateDefinition);

                return Ok(new { message = "Tradie profile picture updated successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception and return a generic error message
                return StatusCode(500, new { message = "An error occurred while updating the profile picture", error = ex.Message });
            }
        }

        [HttpPost("add-tradie-job-ad")]
        public async Task<IActionResult> AddTradieJobAd([FromBody] Services jobPost)
        {
            try
            {
                var tradie = await _context.User.Find(user => user._id == jobPost.UserID && user.Role == "Tradie").FirstOrDefaultAsync();
                if (tradie == null)
                {
                    return NotFound(new object[0]);
                }

                var existingService = await _context.Services.Find(service => service.JobAdTitle == jobPost.JobAdTitle).FirstOrDefaultAsync();
                if (existingService != null)
                {
                    return NotFound(new { message = "Job Post Ad already exists" });
                }

                await _context.Services.InsertOneAsync(jobPost);

                // Add count for published job ad
                if (jobPost.IsActive)
                {
                    var addCountJobPublished = new UpdateCount { TradieID = jobPost.UserID, Count = tradie.ActiveJobs + 1 };
                    await UpdatePublishedAdsCount(addCountJobPublished);
                }

                return Ok(new { message = "Tradie job post added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding job post", error = ex.Message });
            }
        }

        [HttpPost("delete-tradie-job")]
        public async Task<IActionResult> DeleteTradieJobAd(string jobId)
        {
            try
            {
                await _context.Services.DeleteOneAsync(job => job._id == jobId);
                return Ok(new { message = "Job deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while sending job offer", error = ex.Message });
            }
        }

        [HttpGet("publishedAds")]
        public async Task<IActionResult> GetPublishedAds([FromQuery] string userId)
        {
            try
            {
                var publishedJobPost = await _context.Services
                    .Find(service => service.UserID == userId && service.IsActive == true)
                    .ToListAsync();

                return Ok(publishedJobPost);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while getting published job post", error = ex.Message });
            }
        }

        [HttpGet("unpublishedAds")]
        public async Task<IActionResult> GetUnpublishedAds([FromQuery] string userId)
        {
            try
            {
                var unpublishedJobPost = await _context.Services
                    .Find(service => service.UserID == userId && service.IsActive == false)
                    .ToListAsync();

                return Ok(unpublishedJobPost);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while getting unpublished job post", error = ex.Message });
            }
        }

        [HttpPut("job-ads/update-isActive")]
        public async Task<IActionResult> UpdateIsActiveJobAds([FromBody] UpdateJobAdsIsActive updateJobAdsIsActive)
        {
            try
            {
                var publishedJobPost = await _context.Services.Find(service => service._id == updateJobAdsIsActive.JobID).FirstOrDefaultAsync();
                if (publishedJobPost == null)
                {
                    return NotFound(new object[0]);
                }

                var updateDefinition = Builders<Services>.Update.Set(u => u.IsActive, updateJobAdsIsActive.IsActive);
                await _context.Services.UpdateOneAsync(service => service._id == updateJobAdsIsActive.JobID, updateDefinition);
                
                var tradieProfile = await _context.User.Find(u => u._id == publishedJobPost.UserID).FirstOrDefaultAsync();
                if (tradieProfile == null)
                {
                    return NotFound(new object[0]);
                }
                
                var updateDefinition2 = Builders<User>.Update
                    .Set(u => u.LastActivityTimeStamp, DateTime.Now)
                    .Set(u => u.LastActivity, "Publish Job Ad");
                await _context.User.UpdateOneAsync(u => u._id == publishedJobPost.UserID, updateDefinition2);
                

                return Ok(new { message = "Active jobs count successfully updated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating active jobs count", error = ex.Message });
            }
        }

        [HttpPost("job-ad-getDetails-byServiceID")]
        public async Task<IActionResult> GetJobDetailsByServiceId([FromBody] GetJobDetailsByServiceId getJobDetails)
        {
            try
            {
                var service = await _context.Services.Find(service => service._id == getJobDetails.ServiceID).FirstOrDefaultAsync();
                if (service == null)
                {
                    return NotFound(new object[0]);
                }
                return Ok(service);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while getting job post details", error = ex.Message });
            }
        }

        [HttpPut("update-job-ad-Details")]
        public async Task<IActionResult> UpdateJobAdDetails([FromBody] Services updatedJobPost)
        {
            try
            {
                var service = await _context.Services.Find(service => service._id == updatedJobPost._id).FirstOrDefaultAsync();
                if (service == null)
                {
                    return NotFound(new object[0]);
                }

                var updateDefinitions = new List<UpdateDefinition<Services>>();

                // Update only the non-null values from the updatedJobPost
                if (!string.IsNullOrEmpty(updatedJobPost.BusinessPostcode))
                {
                    updateDefinitions.Add(Builders<Services>.Update.Set(u => u.BusinessPostcode, updatedJobPost.BusinessPostcode));
                }

                if (!string.IsNullOrEmpty(updatedJobPost.JobCategory))
                {
                    updateDefinitions.Add(Builders<Services>.Update.Set(u => u.JobCategory, updatedJobPost.JobCategory));
                }

                if (!string.IsNullOrEmpty(updatedJobPost.JobAdTitle))
                {
                    updateDefinitions.Add(Builders<Services>.Update.Set(u => u.JobAdTitle, updatedJobPost.JobAdTitle));
                }

                if (!string.IsNullOrEmpty(updatedJobPost.DescriptionOfService))
                {
                    updateDefinitions.Add(Builders<Services>.Update.Set(u => u.DescriptionOfService, updatedJobPost.DescriptionOfService));
                }

                if (!string.IsNullOrEmpty(updatedJobPost.PricingOption))
                {
                    updateDefinitions.Add(Builders<Services>.Update.Set(u => u.PricingOption, updatedJobPost.PricingOption));
                }

                if (!string.IsNullOrEmpty(updatedJobPost.PricingStartsAt))
                {
                    updateDefinitions.Add(Builders<Services>.Update.Set(u => u.PricingStartsAt, updatedJobPost.PricingStartsAt));
                }

                if (!string.IsNullOrEmpty(updatedJobPost.Currency))
                {
                    updateDefinitions.Add(Builders<Services>.Update.Set(u => u.Currency, updatedJobPost.Currency));
                }

                if (!string.IsNullOrEmpty(updatedJobPost.ThumbnailImage))
                {
                    updateDefinitions.Add(Builders<Services>.Update.Set(u => u.ThumbnailImage, updatedJobPost.ThumbnailImage));
                }

                if (updatedJobPost.ProjectGallery != null)
                {
                    var currentProjectGallery = service.ProjectGallery ?? new List<string>();

                    if (!updatedJobPost.ProjectGallery.SequenceEqual(currentProjectGallery))
                    {
                        updateDefinitions.Add(Builders<Services>.Update.Set(u => u.ProjectGallery, updatedJobPost.ProjectGallery));
                    }
                }

                if (updateDefinitions.Count == 0)
                {
                    return BadRequest(new { message = "No valid fields to update" });
                }

                var updateDefinition = Builders<Services>.Update.Combine(updateDefinitions);
                var filter = Builders<Services>.Filter.Eq(u => u._id, updatedJobPost._id);

                await _context.Services.UpdateOneAsync(filter, updateDefinition);

                var tradie = await _context.User.Find(user => user._id == updatedJobPost.UserID && user.Role == "Tradie").FirstOrDefaultAsync();
                if (tradie == null)
                {
                    return NotFound(new object[0]);
                }
                
                var updateDefinition2 = Builders<User>.Update
                    .Set(u => u.LastActivityTimeStamp, DateTime.Now)
                    .Set(u => u.LastActivity, "Update Job Ad");
                await _context.User.UpdateOneAsync(u => u._id == tradie._id, updateDefinition2);
                

                if (updatedJobPost.IsActive)
                {
                    var addCountJobPublished = new UpdateCount { TradieID = updatedJobPost.UserID, Count = tradie.PublishedAds + 1 };
                    await UpdatePublishedAdsCount(addCountJobPublished);
                }
                else
                {
                    var jobCount = tradie.PublishedAds == 0 ? 0 : tradie.PublishedAds - 1;
                    var addCountJobPublished = new UpdateCount { TradieID = updatedJobPost.UserID, Count = jobCount };
                    await UpdatePublishedAdsCount(addCountJobPublished);
                }

                return Ok(new { message = "Job post ad updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating job post details", error = ex.Message });
            }
        }

        [HttpPost("GetJobsByStatus")]
        public async Task<IActionResult> GetJobsByStatus([FromBody] GetJobsByStatus jobsByStatus)
        {
            try
            {
                // Step 2: Filter jobs based on Status and ClientID
                var jobFilterBuilder = Builders<Jobs>.Filter;
                var jobFilter = jobFilterBuilder.Eq(job => job.Status, jobsByStatus.Status) &
                                jobFilterBuilder.Eq(job => job.TradieID, jobsByStatus.UserID);

                var jobs = await _context.Jobs.Find(jobFilter).ToListAsync();

                return Ok(jobs);
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
                    return Ok(new object[0]);
                }

                var tradie = _context.User.Find(user => user._id == updateJobStatus.TradieID && user.Role.ToLower() == "tradie").FirstOrDefault();
                if (tradie == null)
                {
                    return Ok(new { message = "Tradie not found" });
                }


                // Update the status

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



                if (updateJobStatus.Status.ToLower() == "in progress")
                {
                    var addCountJobActive = new UpdateCount { TradieID = updateJobStatus.TradieID, Count = tradie.ActiveJobs + 1 };
                    UpdateActiveJobsCount(addCountJobActive);
                }  // add count for active jobs or in progress

                if (job.Status.ToLower() == "in progress" && updateJobStatus.Status.ToLower() == "cancelled")
                {
                    var jobCount = tradie.ActiveJobs == 0 ? 0 : tradie.ActiveJobs - 1;
                    var addCountJobActive = new UpdateCount { TradieID = updateJobStatus.TradieID, Count = jobCount };
                    UpdateActiveJobsCount(addCountJobActive);
                } // from in progress to cancelled

                if (updateJobStatus.Status.ToLower() == "completed") // from in progress to completed
                {
                    var addCountJobCompleted = new UpdateCount { TradieID = updateJobStatus.TradieID, Count = tradie.CompletedJobs + 1 };
                    UpdateCompletedJobs(addCountJobCompleted);

                    var earningAmount = Convert.ToDecimal(tradie.EstimatedEarnings) + Convert.ToDecimal(job.ClientBudget);
                    var addEarning = new UpdateEstimatedEarning { TradieID = updateJobStatus.TradieID, Earning = earningAmount };
                    UpdateEstimatedEarningOfTradie(addEarning);
                } //add count for completed jobs and update earning

                return Ok(new { message = "Job status updated successfully" });





            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the profile", error = ex.Message });
            }
        }

        // estimated earning
        private async Task UpdateEstimatedEarningOfTradie([FromBody] UpdateEstimatedEarning updateEarning)
        {
            try
            {
                var tradie = await _context.User.Find(user => user._id == updateEarning.TradieID && user.Role == "Tradie").FirstOrDefaultAsync();
                if (tradie == null)
                {
                    NotFound(new object[0]);
                    return;
                }

                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.EstimatedEarnings, updateEarning.Earning);
                await _context.User.UpdateOneAsync(user => user._id == updateEarning.TradieID, updateDefinition);

                Ok(new { message = "Earning updated: " + updateEarning.Earning.ToString() });
                return;
            }
            catch (Exception ex)
            {
                StatusCode(500, new { message = "An error occurred while updating earning", error = ex.Message });
                return;
            }
        }

        // published ads
        private async Task UpdatePublishedAdsCount([FromBody] UpdateCount updateCount)
        {
            try
            {
                var tradie = await _context.User.Find(user => user._id == updateCount.TradieID && user.Role == "Tradie").FirstOrDefaultAsync();
                if (tradie == null)
                {
                    NotFound(new object[0]);
                    return;
                }

                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.PublishedAds, updateCount.Count);
                await _context.User.UpdateOneAsync(user => user._id == updateCount.TradieID, updateDefinition);

                Ok(new { message = "Published jobs count updated: " + updateCount.Count.ToString() });
                return;
            }
            catch (Exception ex)
            {
                StatusCode(500, new { message = "An error occurred while updating published job count", error = ex.Message });
                return;
            }
        }

        // in progress jobs
        private async Task UpdateActiveJobsCount([FromBody] UpdateCount updateCount)
        {
            try
            {
                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.ActiveJobs, updateCount.Count);
                await _context.User.UpdateOneAsync(user => user._id == updateCount.TradieID, updateDefinition);

                Ok(new { message = "Active jobs count updated: " + updateCount.Count.ToString() });
                return;
            }
            catch (Exception ex)
            {
                StatusCode(500, new { message = "An error occurred while updating active job count", error = ex.Message });
                return;
            }
        }

        // completed jobs
        private async Task UpdateCompletedJobs([FromBody] UpdateCount updateCount)
        {
            try
            {
                // Update the status
                var updateDefinition = Builders<User>.Update.Set(u => u.CompletedJobs, updateCount.Count);
                await _context.User.UpdateOneAsync(user => user._id == updateCount.TradieID, updateDefinition);

                Ok(new { message = "Completed jobs count updated: " + updateCount.Count.ToString() });
                return;
            }
            catch (Exception ex)
            {
                StatusCode(500, new { message = "An error occurred while updating completed job count", error = ex.Message });
                return;
            }
        }

    }
}
