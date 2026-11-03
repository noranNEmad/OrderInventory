using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrderInventory.Api.DTOs.Auth;
using OrderInventory.Api.Identity;
using OrderInventory.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            return BadRequest("Email is already registered.");
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName,
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok("User registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(
            user,
            request.Password);

        if (!isPasswordValid)
        {
            return Unauthorized("Invalid email or password.");
        }

        var token = await _jwtTokenService.CreateToken(user);

        return Ok(new
        {
            message = "Login successful.",
            token = token
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            message = "You are authenticated!",
            username = User.Identity?.Name
        });
    }
}