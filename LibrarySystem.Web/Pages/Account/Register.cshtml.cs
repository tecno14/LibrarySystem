using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Web.Pages.Account;

public class RegisterModel(IAuthService authService, ILogger<RegisterModel> logger) : PageModel
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<RegisterModel> _logger = logger;

    [BindProperty]
    public RegisterViewModel RegisterInput { get; set; }

    public string ReturnUrl { get; set; }

    public void OnGet(string returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        if (ModelState.IsValid)
        {
            var user = await _authService.RegisterAsync(RegisterInput.Username, RegisterInput.Password, RegisterInput.FullName);

            if (user != null)
            {
                _logger.LogInformation("User created a new account with password.");

                // After successful registration, automatically log the user in.
                // This creates a better user experience.
                // We can reuse the LoginModel's logic here for simplicity, or implement it directly.
                // For this example, we'll just redirect to the Login page with a success message.
                TempData["StatusMessage"] = "Registration successful! Please log in.";
                return RedirectToPage("./Login");
            }

            // If registration fails (e.g., username taken), add a model error.
            ModelState.AddModelError(string.Empty, "Registration failed. The username may already be in use.");
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }
}
