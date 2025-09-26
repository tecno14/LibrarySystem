using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace LibrarySystem.Web.Pages.Member;

[Authorize] // Ensures that only authenticated users can access this page.
public class MyBooksModel(IBookService bookService) : PageModel
{
    private readonly IBookService _bookService = bookService;

    // --- Properties to hold the data for the view ---
    public List<Borrowing> CurrentBorrowings { get; set; } = [];
    public List<Borrowing> BorrowingHistory { get; set; } = [];

    /// <summary>
    /// Handles the GET request to display the user's borrowed books.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        // Get the current user's ID from their claims.
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            // If the user ID claim is missing or invalid, it's an error.
            return Forbid(); // Or return a custom error page.
        }

        // Fetch all borrowing records for the current user.
        var allUserBorrowings = await _bookService.GetUserBorrowingHistoryAsync(userId);

        // Use LINQ to partition the borrowings into two lists:
        // 1. Current borrowings (where ReturnDate is null).
        // 2. Past borrowings (where ReturnDate has a value).
        CurrentBorrowings = [.. allUserBorrowings.Where(b => b.ReturnDate == null)];
        BorrowingHistory = [.. allUserBorrowings.Where(b => b.ReturnDate != null)];

        return Page();
    }

    /// <summary>
    /// Handles the POST request to return a book.
    /// </summary>
    /// <param name="borrowingId">The ID of the borrowing record to be finalized.</param>
    public async Task<IActionResult> OnPostReturnAsync(int borrowingId)
    {
        if (borrowingId <= 0)
        {
            return BadRequest();
        }

        var (success, message) = await _bookService.ReturnCopyByBorrowingIdAsync(borrowingId);

        // Use TempData to show a status message to the user after the page reloads.
        TempData["IsSuccess"] = success;
        TempData["StatusMessage"] = message;

        // Redirect back to the same page to prevent form resubmission on refresh.
        return RedirectToPage();
    }
}
