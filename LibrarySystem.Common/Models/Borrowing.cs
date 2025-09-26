namespace LibrarySystem.Common.Models;

public class Borrowing
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CopyId { get; set; }
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }

    // --- Additional properties for JOINed data ---
    // These can be populated by repository methods to avoid extra lookups.
    public string? BookTitle { get; set; }
    public string? BookAuthor { get; set; }
    public string? BookIsbn { get; set; }
    public string? UserFullName { get; set; }
}
