using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkProjectBackend.DbContexts;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SocialNetworkProjectBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly SocialNetworkProjectDbContext _dbContext;

        public ProfileController(SocialNetworkProjectDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public class ProfileDto
        {
            [MaxLength(100)]
            public string? DisplayName { get; set; }

            [MaxLength(500)]
            public string? Bio { get; set; }

            [MaxLength(255)]
            public string? ProfilePictureUrl { get; set; }

            [MaxLength(100)]
            public string? Location { get; set; }

            [DataType(DataType.Date)]
            public DateTime? BirthDate { get; set; }
        }

        [HttpGet]
        [Route("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                string? username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                if (username == null)
                {
                    return Unauthorized();
                }

                if (_dbContext.UserProfiles == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                var profile = await _dbContext.UserProfiles.FindAsync(username);
                if (profile == null)
                {
                    return NotFound($"Profile for user {username} not found.");
                }

                return Ok(new ProfileDto
                {
                    DisplayName = profile.DisplayName,
                    Bio = profile.Bio,
                    ProfilePictureUrl = profile.ProfilePictureUrl,
                    Location = profile.Location,
                    BirthDate = profile.BirthDate
                });
            }
            catch (Exception)
            {
                return Problem(
                    statusCode: 500,
                    title: "Internal server error."
                );
            }
        }

        [HttpGet]
        [Route("{username}")]
        public async Task<IActionResult> GetUserProfile(string username)
        {
            try
            {
                if (_dbContext.UserProfiles == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                var profile = await _dbContext.UserProfiles.FindAsync(username);
                if (profile == null)
                {
                    return NotFound($"Profile for user {username} not found.");
                }

                return Ok(new ProfileDto
                {
                    DisplayName = profile.DisplayName,
                    Bio = profile.Bio,
                    ProfilePictureUrl = profile.ProfilePictureUrl,
                    Location = profile.Location,
                    BirthDate = profile.BirthDate
                });
            }
            catch (Exception)
            {
                return Problem(
                    statusCode: 500,
                    title: "Internal server error."
                );
            }
        }

        [HttpPut]
        [Route("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileDto profileDto)
        {
            try
            {
                string? username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                if (username == null)
                {
                    return Unauthorized();
                }

                if (_dbContext.UserProfiles == null || _dbContext.Users == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                var user = await _dbContext.Users.FindAsync(username);
                if (user == null)
                {
                    return Unauthorized();
                }

                var profile = await _dbContext.UserProfiles.FindAsync(username);
                if (profile == null)
                {
                    // Create new profile if it doesn't exist
                    profile = new SocialNetworkProjectDbContext.UserProfile
                    {
                        Username = username,
                        DisplayName = profileDto.DisplayName,
                        Bio = profileDto.Bio,
                        ProfilePictureUrl = profileDto.ProfilePictureUrl,
                        Location = profileDto.Location,
                        BirthDate = profileDto.BirthDate,
                        LastUpdated = DateTime.UtcNow
                    };
                    _dbContext.UserProfiles.Add(profile);
                }
                else
                {
                    // Update existing profile
                    profile.DisplayName = profileDto.DisplayName;
                    profile.Bio = profileDto.Bio;
                    profile.ProfilePictureUrl = profileDto.ProfilePictureUrl;
                    profile.Location = profileDto.Location;
                    profile.BirthDate = profileDto.BirthDate;
                    profile.LastUpdated = DateTime.UtcNow;
                    _dbContext.UserProfiles.Update(profile);
                }

                await _dbContext.SaveChangesAsync();
                return Ok(profileDto);
            }
            catch (Exception)
            {
                return Problem(
                    statusCode: 500,
                    title: "Internal server error."
                );
            }
        }
    }
}
