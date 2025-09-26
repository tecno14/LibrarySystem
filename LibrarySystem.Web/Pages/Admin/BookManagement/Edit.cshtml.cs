using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Common.Models;
using LibrarySystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Web.Pages.Admin.BookManagement;

[Authorize(Policy = "ManagerOnly")]
public class EditModel(IBookService bookService) : PageModel
{
    private readonly IBookService _bookService = bookService;

    [BindProperty]
    public BookTitleViewModel Book { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _bookService.GetBookTitleByIdAsync(id.Value);
        if (book == null)
        {
            return NotFound();
        }

        // Map from the domain model to the view model
        Book = new BookTitleViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            ISBN = book.ISBN,
            Genre = book.Genre,
            Publisher = book.Publisher,
            PublicationYear = book.PublicationYear,
            CoverImageUrl = book.CoverImageUrl
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Map from the view model back to the domain model
        var bookToUpdate = new Book
        {
            Id = Book.Id,
            Title = Book.Title,
            Author = Book.Author,
            ISBN = Book.ISBN,
            Genre = Book.Genre,
            Publisher = Book.Publisher,
            PublicationYear = Book.PublicationYear,
            CoverImageUrl = Book.CoverImageUrl
        };

        await _bookService.UpdateTitleDetailsAsync(bookToUpdate);

        TempData["StatusMessage"] = $"Book title '{bookToUpdate.Title}' was successfully updated.";
        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostArchiveAsync()
    {
        await _bookService.ArchiveTitleAsync(Book.Id);
        TempData["StatusMessage"] = $"Book ID {Book.Id} has been archived.";
        return RedirectToPage("Index");
    }
}
