using System.Net.Mail;
using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.Core.Validation;

public static class ClientValidator
{
    public const int FullNameMaxLength = 150;
    public const int PhoneMaxLength = 50;
    public const int EmailMaxLength = 254;
    public const int NotesMaxLength = 2_000;

    public static IReadOnlyDictionary<string, string> Validate(ClientInput input)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(input.FullName))
        {
            errors[nameof(input.FullName)] = "Full name is required.";
        }
        else if (input.FullName.Trim().Length > FullNameMaxLength)
        {
            errors[nameof(input.FullName)] = $"Full name must be at most {FullNameMaxLength} characters.";
        }

        if (input.Phone?.Trim().Length > PhoneMaxLength)
        {
            errors[nameof(input.Phone)] = $"Phone must be at most {PhoneMaxLength} characters.";
        }

        var email = input.Email?.Trim();
        if (email?.Length > EmailMaxLength)
        {
            errors[nameof(input.Email)] = $"Email must be at most {EmailMaxLength} characters.";
        }
        else if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
        {
            errors[nameof(input.Email)] = "Enter a valid email address.";
        }

        if (input.Notes?.Trim().Length > NotesMaxLength)
        {
            errors[nameof(input.Notes)] = $"Notes must be at most {NotesMaxLength} characters.";
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            return new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
