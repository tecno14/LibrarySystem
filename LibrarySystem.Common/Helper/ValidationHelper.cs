using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Common.Helper;

public static class ValidationHelper
{
    public static void ValidateObject(object obj)
    {
        var context = new ValidationContext(obj);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(obj, context, results, true))
        {
            var messages = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ValidationException($"Validation failed: {messages}");
        }
    }
}
