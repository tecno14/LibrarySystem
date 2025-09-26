using LibrarySystem.Common.Validation;
using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Common.ValidationAttributes;

public class IsbnFormatAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string isbn || string.IsNullOrWhiteSpace(isbn))
            return ValidationResult.Success; // Let [Required] handle null/empty

        return IsbnValidator.IsValid(isbn)
            ? ValidationResult.Success
            : new ValidationResult("Invalid ISBN format. Must be a valid ISBN-10 or ISBN-13.");
    }
}
