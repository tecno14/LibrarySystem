using LibrarySystem.Common.DTOs;

namespace LibrarySystem.Common.Models.MappingExtenstions;

public static class BookMappingExtensions
{
    public static Book ToEntity(this BookInputDto dto) => new()
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
