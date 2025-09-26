using Bogus;
using LibrarySystem.BLL.Constants;
using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Common.Models;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.BLL.Development;

public class SampleDataSeeder(
        IBookRepository bookRepository,
        IBookCopyRepository bookCopyRepository,
        ICurrentUserService currentUser,
        ICacheService cache,
        ILogger<SampleDataSeeder> logger) : ISampleDataSeeder
{
    private readonly IBookRepository _bookRepository = bookRepository;
    private readonly IBookCopyRepository _bookCopyRepository = bookCopyRepository;
    private readonly ICacheService _cache = cache;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<SampleDataSeeder> _logger = logger;

    public async Task<bool> SeedCatalogAsync(int titleCount, int maxCopiesPerTitle)
    {
        if (!_currentUser.IsPowerUser)
            throw new UnauthorizedAccessException("Only moderators can do this action.");

        try
        {
            if (await _bookRepository.GetTotalCountAsync(includeHidden: true) > 0)
            {
                _logger.LogWarning("Catalog seeding skipped — books already exist.");
                return false;
            }

            var bookFaker = new Faker<Book>()
                .RuleFor(b => b.Title, f => f.Commerce.ProductName())
                .RuleFor(b => b.Author, f => f.Name.FullName())
                .RuleFor(b => b.ISBN, f => f.Commerce.Ean13())
                .RuleFor(b => b.Publisher, f => f.Company.CompanyName())
                .RuleFor(b => b.Genre, f => f.PickRandom("Fantasy", "Science Fiction", "Mystery"));

            var bookTitles = bookFaker.Generate(titleCount);
            foreach (var title in bookTitles)
            {
                await _bookRepository.AddAsync(title);

                var copyCount = new Random().Next(1, maxCopiesPerTitle + 1);
                for (int i = 0; i < copyCount; i++)
                {
                    var newCopy = new BookCopy
                    {
                        ISBN = title.ISBN,
                        Status = Common.Enums.BookCopyStatus.Available
                    };
                    await _bookCopyRepository.AddAsync(newCopy);
                }
            }

            _cache.Invalidate(CacheKeys.MasterBookList);
            _logger.LogInformation("Catalog seeded successfully with {TitleCount} titles and up to {MaxCopies} copies each.", titleCount, maxCopiesPerTitle);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding catalog.");
            throw;
        }
    }
}
