namespace LibrarySystem.Common.Models;

public class Book
{
    public int Id { get; set; }
    public string ISBN { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Author { get; set; } = default!;
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? Genre { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsHidden { get; set; }
    public DateTime CreatedAt { get; set; }
}
