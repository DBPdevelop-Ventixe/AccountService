using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApi.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AccountController(ProtoProfileService.ProtoProfileServiceClient profilesService) : ControllerBase
{
    private readonly ProtoProfileService.ProtoProfileServiceClient _profilesService = profilesService;

    [Authorize]
    [HttpGet("/api/token/validate")]
    public IActionResult CheckAccess()
    {
        return Ok(new { message = "You are still signed in" });
    }


    [Authorize]
    [HttpPost("/api/profile/add")]
    public async Task<IActionResult> AddProfile([FromBody] AddProfileRequest request)
    {
        if(request  == null)
            return BadRequest(new { message = "Request can't be null" });

        // Get user from token
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest();

        Console.WriteLine($"! - {userId} requested to add profile.");

        // Set userName as id to the request
        request.Userid = userId;

        var response = await _profilesService.AddProfileAsync(request);
        if (!response.Success)
        {
            return BadRequest(new { message = response.Message });
        }

        return Ok(new { message = "Account created successfully" });
    }


    [Authorize]
    [HttpGet("/api/profile/get")]
    public async Task<IActionResult> GetProfile()
    {
        // Get user from token
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest();

        Console.WriteLine($"! - Account {userId} requested profile info.");
        GetProfileRequest request = new GetProfileRequest
        {
            UserId = userId
        };


        var response = await _profilesService.GetProfileAsync(request);
        if (response == null)
        {
            return NotFound(new { message = "Profile not found" });
        }

        return Ok(response);
    }


    [Authorize]
    [HttpPut("/api/profile/update")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Request can't be null" });

        // Get user from token
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest();

        Console.WriteLine($"! - Account {userId} requested to update profile.");

        // Set userName as id to the request
        request.UserId = userId;
        var response = await _profilesService.UpdateProfileAsync(request);
        if (!response.Success)
        {
            return BadRequest(new { message = response.Message });
        }
        return Ok(new { message = "Account updated successfully" });
    }
}
