using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Web.ViewModels;

/// <summary>
/// Represents the data required for a user to log in.
/// This model is used by the Login Razor Page.
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "A username is required.")]
    [Display(Name = "Username")]
    public string Username { get; set; }

    [Required(ErrorMessage = "A password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }
}