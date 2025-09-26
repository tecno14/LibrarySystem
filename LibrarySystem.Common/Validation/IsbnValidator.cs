using System.Text.RegularExpressions;

namespace LibrarySystem.Common.Validation;

public static class IsbnValidator
{
    public static bool IsValid(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn)) return false;

        isbn = isbn.Replace("-", "").ToUpper();
        return IsValidIsbn10(isbn) || IsValidIsbn13(isbn);
    }

    private static bool IsValidIsbn10(string isbn)
    {
        if (isbn.Length != 10 || !Regex.IsMatch(isbn.Substring(0, 9), @"^\d{9}$")) return false;

        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (i + 1) * (isbn[i] - '0');

        sum += isbn[9] == 'X' ? 10 * 10 : 10 * (isbn[9] - '0');
        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        if (isbn.Length != 13 || !isbn.All(char.IsDigit)) return false;

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = isbn[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == (isbn[12] - '0');
    }
}
