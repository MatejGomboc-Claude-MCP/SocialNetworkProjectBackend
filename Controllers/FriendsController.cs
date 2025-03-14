using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetworkProjectBackend.DbContexts;
using System.Security.Claims;

namespace SocialNetworkProjectBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly SocialNetworkProjectDbContext _dbContext;

        public FriendsController(SocialNetworkProjectDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public class FriendDto
        {
            public string Username { get; set; } = string.Empty;
            public string? DisplayName { get; set; }
            public string? ProfilePictureUrl { get; set; }
            public DateTime FriendsSince { get; set; }
        }

        public class FriendRequestDto
        {
            public Guid Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string? DisplayName { get; set; }
            public string? ProfilePictureUrl { get; set; }
            public DateTime RequestDate { get; set; }
            public string Status { get; set; } = "Pending";
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            try
            {
                string? username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                if (username == null)
                {
                    return Unauthorized();
                }

                if (_dbContext.FriendRequests == null || _dbContext.UserProfiles == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                // Get accepted friend requests where the user is either sender or receiver
                var friendRequests = await _dbContext.FriendRequests
                    .Where(fr => 
                        (fr.SenderUsername == username || fr.ReceiverUsername == username) && 
                        fr.Status == SocialNetworkProjectDbContext.FriendRequestStatus.Accepted)
                    .ToListAsync();

                var friendDtos = new List<FriendDto>();

                foreach (var request in friendRequests)
                {
                    // Determine the friend's username (opposite of the current user)
                    string friendUsername = request.SenderUsername == username ? 
                        request.ReceiverUsername : request.SenderUsername;
                    
                    // Get the friend's profile
                    var profile = await _dbContext.UserProfiles.FindAsync(friendUsername);
                    
                    friendDtos.Add(new FriendDto
                    {
                        Username = friendUsername,
                        DisplayName = profile?.DisplayName,
                        ProfilePictureUrl = profile?.ProfilePictureUrl,
                        FriendsSince = request.RespondedAt ?? request.CreatedAt
                    });
                }

                return Ok(friendDtos);
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
        [Route("requests/received")]
        public async Task<IActionResult> GetReceivedFriendRequests()
        {
            try
            {
                string? username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                if (username == null)
                {
                    return Unauthorized();
                }

                if (_dbContext.FriendRequests == null || _dbContext.UserProfiles == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                // Get pending friend requests where the user is the receiver
                var requests = await _dbContext.FriendRequests
                    .Where(fr => fr.ReceiverUsername == username && 
                                fr.Status == SocialNetworkProjectDbContext.FriendRequestStatus.Pending)
                    .ToListAsync();

                var requestDtos = new List<FriendRequestDto>();

                foreach (var request in requests)
                {
                    // Get the sender's profile
                    var profile = await _dbContext.UserProfiles.FindAsync(request.SenderUsername);
                    
                    requestDtos.Add(new FriendRequestDto
                    {
                        Id = request.Id,
                        Username = request.SenderUsername,
                        DisplayName = profile?.DisplayName,
                        ProfilePictureUrl = profile?.ProfilePictureUrl,
                        RequestDate = request.CreatedAt,
                        Status = request.Status.ToString()
                    });
                }

                return Ok(requestDtos);
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
        [Route("requests/sent")]
        public async Task<IActionResult> GetSentFriendRequests()
        {
            try
            {
                string? username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                if (username == null)
                {
                    return Unauthorized();
                }

                if (_dbContext.FriendRequests == null || _dbContext.UserProfiles == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                // Get pending friend requests where the user is the sender
                var requests = await _dbContext.FriendRequests
                    .Where(fr => fr.SenderUsername == username && 
                                fr.Status == SocialNetworkProjectDbContext.FriendRequestStatus.Pending)
                    .ToListAsync();

                var requestDtos = new List<FriendRequestDto>();

                foreach (var request in requests)
                {
                    // Get the receiver's profile
                    var profile = await _dbContext.UserProfiles.FindAsync(request.ReceiverUsername);
                    
                    requestDtos.Add(new FriendRequestDto
                    {
                        Id = request.Id,
                        Username = request.ReceiverUsername,
                        DisplayName = profile?.DisplayName,
                        ProfilePictureUrl = profile?.ProfilePictureUrl,
                        RequestDate = request.CreatedAt,
                        Status = request.Status.ToString()
                    });
                }

                return Ok(requestDtos);
            }
            catch (Exception)
            {
                return Problem(
                    statusCode: 500,
                    title: "Internal server error."
                );
            }
        }

        [HttpPost]
        [Route("requests/{friendUsername}")]
        public async Task<IActionResult> SendFriendRequest(string friendUsername)
        {
            try
            {
                string? username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                if (username == null)
                {
                    return Unauthorized();
                }

                if (username == friendUsername)
                {
                    return BadRequest("You cannot send a friend request to yourself.");
                }

                if (_dbContext.FriendRequests == null || _dbContext.Users == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                // Check if friend exists
                var friend = await _dbContext.Users.FindAsync(friendUsername);
                if (friend == null)
                {
                    return NotFound($"User {friendUsername} not found.");
                }

                // Check if a friend request already exists
                var existingRequest = await _dbContext.FriendRequests
                    .Where(fr => 
                        (fr.SenderUsername == username && fr.ReceiverUsername == friendUsername) ||
                        (fr.SenderUsername == friendUsername && fr.ReceiverUsername == username))
                    .FirstOrDefaultAsync();

                if (existingRequest != null)
                {
                    if (existingRequest.Status == SocialNetworkProjectDbContext.FriendRequestStatus.Accepted)
                    {
                        return BadRequest($"You are already friends with {friendUsername}.");
                    }
                    else if (existingRequest.Status == SocialNetworkProjectDbContext.FriendRequestStatus.Pending)
                    {
                        if (existingRequest.SenderUsername == username)
                        {
                            return BadRequest($"You have already sent a friend request to {friendUsername}.");
                        }
                        else
                        {
                            return BadRequest($"{friendUsername} has already sent you a friend request. Please check your received requests.");
                        }
                    }
                    else if (existingRequest.Status == SocialNetworkProjectDbContext.FriendRequestStatus.Rejected)
                    {
                        // If there was a previous rejected request, update it to pending
                        existingRequest.Status = SocialNetworkProjectDbContext.FriendRequestStatus.Pending;
                        existingRequest.CreatedAt = DateTime.UtcNow;
                        existingRequest.RespondedAt = null;
                        _dbContext.FriendRequests.Update(existingRequest);
                        await _dbContext.SaveChangesAsync();
                        
                        return Ok(new { Message = $"Friend request sent to {friendUsername}." });
                    }
                }

                // Create a new friend request
                var newRequest = new SocialNetworkProjectDbContext.FriendRequest
                {
                    Id = Guid.NewGuid(),
                    SenderUsername = username,
                    ReceiverUsername = friendUsername,
                    Status = SocialNetworkProjectDbContext.FriendRequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.FriendRequests.Add(newRequest);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = $"Friend request sent to {friendUsername}." });
            }
            catch (Exception)
            {
                return Problem(
                    statusCode: 500,
                    title: "Internal server error."
                );
            }
        }

        [HttpPatch]
        [Route("requests/{requestId}/accept")]
        public async Task<IActionResult> AcceptFriendRequest(Guid requestId)
        {
            try
            {
                string? username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                if (username == null)
                {
                    return Unauthorized();
                }

                if (_dbContext.FriendRequests == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                var request = await _dbContext.FriendRequests.FindAsync(requestId);
                if (request == null)
                {
                    return NotFound("Friend request not found.");
                }

                // Verify that the current user is the receiver of the request
                if (request.ReceiverUsername != username)
                {
                    return Unauthorized("You can only accept friend requests sent to you.");
                }

                // Check if the request is pending
                if (request.Status != SocialNetworkProjectDbContext.FriendRequestStatus.Pending)
                {
                    return BadRequest("This friend request has already been processed.");
                }

                // Accept the friend request
                request.Status = SocialNetworkProjectDbContext.FriendRequestStatus.Accepted;
                request.RespondedAt = DateTime.UtcNow;
                
                _dbContext.FriendRequests.Update(request);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = $"Friend request from {request.SenderUsername} accepted." });
            }
            catch (Exception)
            {
                return Problem(
                    statusCode: 500,
                    title: "Internal server error."
                );
            }
        }

        [HttpPatch]
        [Route("requests/{requestId}/reject")]
        public async Task<IActionResult> RejectFriendRequest(Guid requestId)
        {
            try
            {
                string? username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                if (username == null)
                {
                    return Unauthorized();
                }

                if (_dbContext.FriendRequests == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                var request = await _dbContext.FriendRequests.FindAsync(requestId);
                if (request == null)
                {
                    return NotFound("Friend request not found.");
                }

                // Verify that the current user is the receiver of the request
                if (request.ReceiverUsername != username)
                {
                    return Unauthorized("You can only reject friend requests sent to you.");
                }

                // Check if the request is pending
                if (request.Status != SocialNetworkProjectDbContext.FriendRequestStatus.Pending)
                {
                    return BadRequest("This friend request has already been processed.");
                }

                // Reject the friend request
                request.Status = SocialNetworkProjectDbContext.FriendRequestStatus.Rejected;
                request.RespondedAt = DateTime.UtcNow;
                
                _dbContext.FriendRequests.Update(request);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = $"Friend request from {request.SenderUsername} rejected." });
            }
            catch (Exception)
            {
                return Problem(
                    statusCode: 500,
                    title: "Internal server error."
                );
            }
        }

        [HttpDelete]
        [Route("{friendUsername}")]
        public async Task<IActionResult> RemoveFriend(string friendUsername)
        {
            try
            {
                string? username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                if (username == null)
                {
                    return Unauthorized();
                }

                if (_dbContext.FriendRequests == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                // Find the friendship
                var friendship = await _dbContext.FriendRequests
                    .Where(fr => 
                        ((fr.SenderUsername == username && fr.ReceiverUsername == friendUsername) ||
                        (fr.SenderUsername == friendUsername && fr.ReceiverUsername == username)) &&
                        fr.Status == SocialNetworkProjectDbContext.FriendRequestStatus.Accepted)
                    .FirstOrDefaultAsync();

                if (friendship == null)
                {
                    return NotFound($"You are not friends with {friendUsername}.");
                }

                // Remove the friendship
                _dbContext.FriendRequests.Remove(friendship);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = $"Removed {friendUsername} from your friends." });
            }
            catch (Exception)
            {
                return Problem(
                    statusCode: 500,
                    title: "Internal server error."
                );
            }
        }

        [HttpDelete]
        [Route("requests/{requestId}")]
        public async Task<IActionResult> CancelFriendRequest(Guid requestId)
        {
            try
            {
                string? username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                if (username == null)
                {
                    return Unauthorized();
                }

                if (_dbContext.FriendRequests == null)
                {
                    return Problem(
                        statusCode: 500,
                        title: "Failed to access database."
                    );
                }

                var request = await _dbContext.FriendRequests.FindAsync(requestId);
                if (request == null)
                {
                    return NotFound("Friend request not found.");
                }

                // Verify that the current user is the sender of the request
                if (request.SenderUsername != username)
                {
                    return Unauthorized("You can only cancel friend requests you have sent.");
                }

                // Check if the request is pending
                if (request.Status != SocialNetworkProjectDbContext.FriendRequestStatus.Pending)
                {
                    return BadRequest("This friend request has already been processed and cannot be canceled.");
                }

                // Remove the request
                _dbContext.FriendRequests.Remove(request);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = $"Friend request to {request.ReceiverUsername} canceled." });
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
