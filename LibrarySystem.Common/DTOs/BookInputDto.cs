using LibrarySystem.Common.Models;
using LibrarySystem.Common.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Common.DTOs;

public class BookInputDto
{
    [Required]
    [StringLength(20, MinimumLength = 10)]
    [IsbnFormat]
    public string ISBN { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; }

    [Required]
    [StringLength(255)]
    public string Author { get; set; }

    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? Genre { get; set; }
    public string? CoverImageUrl { get; set; }

    public static Book ToEntity(BookInputDto dto) => new()
    {
        ISBN = dto.ISBN,
        Title = dto.Title,
        Author = dto.Author,
        Publisher = dto.Publisher,
        PublicationYear = dto.PublicationYear,
        Genre = dto.Genre,
        CoverImageUrl = dto.CoverImageUrl,
        CreatedAt = DateTime.UtcNow
    };
}
