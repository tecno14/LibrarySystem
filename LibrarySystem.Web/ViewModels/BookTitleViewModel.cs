using LibrarySystem.Common.DTOs;
using LibrarySystem.Common.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Web.ViewModels;

/// <summary>
/// Represents the data needed to create or edit a book title's metadata.
/// This model is used by the Create and Edit pages in the Admin/BookManagement folder.
/// </summary>
public class BookTitleViewModel
{
    /// <summary>
    /// The ID of the book. Will be 0 when creating a new book.
    /// </summary>
    public int Id { get; set; }

    [Required]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "The ISBN must be between 10 and 20 characters.")]
    [IsbnFormat]
    public string ISBN { get; set; }

    [Required]
    [StringLength(255, ErrorMessage = "The title cannot exceed 255 characters.")]
    public string Title { get; set; }

    [Required]
    [StringLength(255, ErrorMessage = "The author's name cannot exceed 255 characters.")]
    public string Author { get; set; }

    [StringLength(150)]
    public string? Publisher { get; set; }

    [Display(Name = "Publication Year")]
    [Range(1400, 2100, ErrorMessage = "Please enter a valid year.")]
    public int? PublicationYear { get; set; }

    [StringLength(100)]
    public string? Genre { get; set; }

    [Display(Name = "Cover Image URL")]
    [Url(ErrorMessage = "Please enter a valid URL for the cover image.")]
    [StringLength(500)]
    public string? CoverImageUrl { get; set; }
}
