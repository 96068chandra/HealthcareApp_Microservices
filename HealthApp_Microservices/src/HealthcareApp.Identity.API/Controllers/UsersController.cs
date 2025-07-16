using HealthcareApp.Common.Domain.Interfaces;

using HealthcareApp.Identity.API.Models.Entities;
using HealthcareApp.Identity.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthcareApp.Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IBaseRepository<ApplicationUser> _userRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ITokenService _tokenService;

    public UsersController(
        IBaseRepository<ApplicationUser> userRepository,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationUser>>> GetUsers()
    {
        var users = await _userRepository.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationUser>> GetUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return user;
    }


    [HttpPost("register")]
    public async Task<ActionResult<ApplicationUser>> Register([FromBody] ApplicationUser user)
    {
        // Check for duplicate email/username
        var existingUsers = await _userRepository.GetAsync(u => u.Email == user.Email || u.Username == user.Username);
        if (existingUsers.Any())
        {
            return BadRequest("Email or Username already exists.");
        }

        user.Id = Guid.NewGuid();
        user.CreatedDate = DateTime.UtcNow;
        user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);

        var createdUser = await _userRepository.AddAsync(user);
        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login([FromBody] LoginRequest request)
    {
        var users = await _userRepository.GetAsync(u => u.Email == request.Email);
        var user = users.FirstOrDefault();
        if (user == null)
        {
            return Unauthorized("Invalid credentials.");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid credentials.");
        }

        var token = _tokenService.GenerateToken(user);
        return Ok(token);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, ApplicationUser user)
    {
        if (id != user.Id)
        {
            return BadRequest();
        }

        await _userRepository.UpdateAsync(user);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        await _userRepository.DeleteAsync(user);
        return NoContent();
    }
}
