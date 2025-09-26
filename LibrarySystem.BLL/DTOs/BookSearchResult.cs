namespace LibrarySystem.BLL.DTOs;

// Represents the combination of a book's metadata and its copy availability.
public class BookSearchResult
{
    // From the 'Books' table
    public int BookId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Author { get; set; } = default!;
    public string ISBN { get; set; } = default!;
    public string? Genre { get; set; }
    public string? Publisher { get; set; }
    public string? CoverImageUrl { get; set; }

    // Calculated from the 'BookCopies' table
    public int AvailableCopies { get; set; }
}
