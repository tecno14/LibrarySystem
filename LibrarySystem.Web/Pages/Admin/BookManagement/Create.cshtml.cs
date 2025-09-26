using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Common.Models;
using LibrarySystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Web.Pages.Admin.BookManagement;

[Authorize(Policy = "ManagerOnly")]
public class CreateModel(IBookService bookService) : PageModel
{
    private readonly IBookService _bookService = bookService;

    [BindProperty]
    public BookTitleViewModel Book { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var newBook = new Book
        {
            Title = Book.Title,
            Author = Book.Author,
            ISBN = Book.ISBN,
            Genre = Book.Genre,
            Publisher = Book.Publisher,
            PublicationYear = Book.PublicationYear,
            CoverImageUrl = Book.CoverImageUrl
        };

        try
        {
            await _bookService.AddTitleToCatalogAsync(newBook);
            TempData["StatusMessage"] = $"Book title '{newBook.Title}' was successfully created.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Book.ISBN", ex.Message);
            return Page();
        }
        catch (Exception)
        {
            // Log unexpected errors and show generic message
            ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
            // Optionally log: _logger.LogError(ex, "Error creating book");
            return Page();
        }
    }
}
