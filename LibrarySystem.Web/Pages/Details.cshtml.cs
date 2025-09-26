using System.Security.Claims;
using LibrarySystem.BLL.DTOs;
using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Web.Pages;

public class DetailsModel(IBookService bookService) : PageModel
{
    private readonly IBookService _bookService = bookService;

    // --- Properties ---
    public BookSearchResult? Book { get; set; }
    public IEnumerable<BookCopy> Copies { get; set; } = [];

    /// <summary>
    /// Fetches the details for a single book based on the ISBN provided in the route.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(string isbn)
    {
        if (string.IsNullOrEmpty(isbn))
        {
            return NotFound();
        }

        // Search for the book by its ISBN to get the main details and availability.
        var searchResult = await _bookService.SearchCatalogAsync(query: isbn);
        Book = searchResult.FirstOrDefault();

        if (Book == null)
        {
            return NotFound();
        }

        // Fetch all physical copies of this book to display their status.
        Copies = await _bookService.GetCopiesByIsbnAsync(isbn);

        return Page();
    }

    /// <summary>
    /// Handles the POST request when a user clicks the "Borrow" button.
    /// </summary>
    public async Task<IActionResult> OnPostBorrowAsync(string isbn)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Challenge(); // User is not logged in, redirect to login.
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            // This should not happen for a logged-in user with a valid claim.
            TempData["IsSuccess"] = false;
            TempData["StatusMessage"] = "Error: Your user ID could not be determined.";
            return RedirectToPage(new { isbn });
        }

        var (success, message) = await _bookService.BorrowCopyByIsbnAsync(isbn, userId, 7);

        // Use TempData to show a message on the page after redirecting.
        TempData["IsSuccess"] = success;
        TempData["StatusMessage"] = message;

        return RedirectToPage(new { isbn });
    }
}
