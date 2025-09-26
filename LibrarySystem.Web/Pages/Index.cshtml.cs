using LibrarySystem.BLL.DTOs;
using LibrarySystem.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibrarySystem.Web.Pages;

public class IndexModel(IBookService bookService) : PageModel
{
    const int pageSize = 12;

    private readonly IBookService _bookService = bookService;

    // --- Properties ---

    /// <summary>
    /// Holds the list of books to display on the page.
    /// </summary>
    public IEnumerable<BookSearchResult> SearchResults { get; set; } = [];

    /// <summary>
    /// Populates the genre dropdown in the search form.
    /// </summary>
    public SelectList GenreOptions { get; set; }

    // --- Bind Properties for Search Form ---

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Genre { get; set; }

    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }

    /// <summary>
    /// Handles the HTTP GET request for the page, including search queries.
    /// </summary>
    public async Task OnGetAsync(int pageNumber = 1)
    {
        // Populate the genre dropdown.
        var genres = await _bookService.GetAllGenresAsync();
        GenreOptions = new SelectList(genres);
        
        var totalCount = await _bookService.GetTotalCountAsync();
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Fetch the results from the BLL based on user's search criteria.
        SearchResults = await _bookService.SearchCatalogAsync(Genre, Query, pageNumber, pageSize);

        CurrentPage = pageNumber;
    }
}
