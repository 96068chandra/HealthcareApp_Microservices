using HealthcareApp.Identity.API.Models.Entities;

namespace HealthcareApp.Identity.API.Services;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user);
}
