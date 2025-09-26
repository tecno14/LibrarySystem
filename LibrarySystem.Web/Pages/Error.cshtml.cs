using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace LibrarySystem.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel(IWebHostEnvironment env) : PageModel
{
    private readonly IWebHostEnvironment _env = env;

    public string? RequestId { get; set; }

    public bool ShowRequestId => _env.IsDevelopment();

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
