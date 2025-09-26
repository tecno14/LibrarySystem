using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Common.Enums;
using LibrarySystem.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Web.Pages.Admin.BookManagement
{
    [Authorize(Policy = "ManagerOnly")]
    public class ManageCopiesModel(IBookService bookService) : PageModel
    {
        private readonly IBookService _bookService = bookService;

        public Book? Book { get; set; }
        public List<BookCopy> Copies { get; set; } = [];

        [BindProperty]
        [Display(Name = "New Copy Location")]
        public string? NewCopyLocation { get; set; }

        public async Task<IActionResult> OnGetAsync(int bookId)
        {
            Book = await _bookService.GetBookTitleByIdAsync(bookId);
            if (Book == null)
            {
                return NotFound();
            }

            Copies = [.. (await _bookService.GetCopiesByIsbnAsync(Book.ISBN))];
            return Page();
        }

        public async Task<IActionResult> OnPostAddCopyAsync(int bookId, string isbn)
        {
            await _bookService.AddNewCopyToInventoryAsync(isbn, NewCopyLocation);

            TempData["StatusMessage"] = $"A new copy for ISBN {isbn} has been added.";
            return RedirectToPage(new { bookId });
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int bookId, int copyId, BookCopyStatus newStatus)
        {
            await _bookService.UpdateCopyStatusAsync(copyId, newStatus);
            TempData["StatusMessage"] = $"Status for copy ID {copyId} has been updated.";
            return RedirectToPage(new { bookId });
        }
    }
}
