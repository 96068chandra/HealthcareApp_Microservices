using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthcareApp.Common.Domain.Interfaces;
using HealthcareApp.Identity.API.Models.Entities;
using HealthcareApp.Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthcareApp.Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        [AllowAnonymous]
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
            user.LastModifiedDate=DateTime.UtcNow;
            
            user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);

            var createdUser = await _userRepository.AddAsync(user);
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
        }

        // ...existing code...

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<string>> Login([FromBody] Models.Dtos.LoginRequest request)
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

        // User profile management
        [HttpGet("profile")]
        public async Task<ActionResult<ApplicationUser>> GetProfile()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            return user;
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ApplicationUser updatedUser)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || updatedUser.Id.ToString() != userId) return Unauthorized();
            await _userRepository.UpdateAsync(updatedUser);
            return NoContent();
        }

        // Update password
        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] Models.Dtos.UpdatePasswordRequest request)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (result == PasswordVerificationResult.Failed) return BadRequest("Current password is incorrect.");
            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            await _userRepository.UpdateAsync(user);
            return NoContent();
        }

        // Email confirmation (token-based, simplified)
        [HttpPost("send-confirmation-email")]
        [AllowAnonymous]
        public IActionResult SendConfirmationEmail([FromBody] string email)
        {
            // In production, generate a token and send email
            // Here, just return a fake token for demonstration
            var token = Guid.NewGuid().ToString();
            return Ok(new { email, token });
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] Models.Dtos.ConfirmEmailRequest request)
        {
            // In production, validate the token
            var users = await _userRepository.GetAsync(u => u.Email == request.Email);
            var user = users.FirstOrDefault();
            if (user == null) return NotFound();
            user.EmailConfirmed = true;
            await _userRepository.UpdateAsync(user);
            return Ok();
        }

        // Password reset (token-based, simplified)
        [HttpPost("send-reset-password-email")]
        [AllowAnonymous]
        public IActionResult SendResetPasswordEmail([FromBody] string email)
        {
            // In production, generate a token and send email
            // Here, just return a fake token for demonstration
            var token = Guid.NewGuid().ToString();
            return Ok(new { email, token });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] Models.Dtos.ResetPasswordRequest request)
        {
            // In production, validate the token
            var users = await _userRepository.GetAsync(u => u.Email == request.Email);
            var user = users.FirstOrDefault();
            if (user == null) return NotFound();
            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            await _userRepository.UpdateAsync(user);
            return Ok();
        }

        // ...existing code...
    }
}
