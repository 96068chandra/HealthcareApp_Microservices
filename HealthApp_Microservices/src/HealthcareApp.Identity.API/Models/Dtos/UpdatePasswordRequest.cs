namespace HealthcareApp.Identity.API.Models.Dtos
{
    public class UpdatePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
