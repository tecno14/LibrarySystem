
namespace LibrarySystem.BLL.Interfaces;

public interface ISampleDataSeeder
{
    Task<bool> SeedCatalogAsync(int titleCount, int maxCopiesPerTitle);
}
