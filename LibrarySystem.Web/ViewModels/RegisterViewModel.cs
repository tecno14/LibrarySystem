using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Web.ViewModels;

/// <summary>
/// Represents the data required for a new user to register.
/// This model is used by the Register Razor Page.
/// </summary>
public class RegisterViewModel
{
    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
    [Display(Name = "Username")]
    public string Username { get; set; }

    [Required]
    [StringLength(200, ErrorMessage = "Your full name cannot exceed 200 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }
}
