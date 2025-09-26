using LibrarySystem.Common.DTOs;

namespace LibrarySystem.Web.ViewModels.MappingExtenstions;

public static class BookMappingExtensions
{
    public static BookInputDto ToDto(this BookTitleViewModel vm) => new()
    {
        ISBN = vm.ISBN,
        Title = vm.Title,
        Author = vm.Author,
        Publisher = vm.Publisher,
        PublicationYear = vm.PublicationYear,
        Genre = vm.Genre,
        CoverImageUrl = vm.CoverImageUrl
    };
}
