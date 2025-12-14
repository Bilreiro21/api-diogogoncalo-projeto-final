using System.ComponentModel.DataAnnotations; // Para [Required], [EmailAddress]

namespace ApiDiogoGoncaloProjetoFinal.DTOs
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")] // Garantimos que é igual à "Password"
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}