using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Web.Pages.Admin.BookManagement;

[Authorize(Policy = "ManagerOnly")]
public class IndexModel(IBookService bookService) : PageModel
{
    const int pageSize = 20;

    private readonly IBookService _bookService = bookService;

    public IEnumerable<Book> Books { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }

    public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
    {
        var totalCount = await _bookService.GetTotalCountAsync();
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        Books = await _bookService.GetPagedAsync(pageNumber, pageSize);

        CurrentPage = pageNumber;

        return Page();
    }
}
