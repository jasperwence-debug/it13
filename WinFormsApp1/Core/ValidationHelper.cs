using System;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace App.WinForms.Core
{
    /// <summary>
    /// Enterprise Input Validation & Error Prevention Helper.
    /// Enforces:
    ///   - Digits-only / international phone validation.
    ///   - RFC-compliant email verification.
    ///   - Schema-matching max length checks.
    ///   - Non-negative numeric price validation.
    ///   - Logical date boundaries (future preferred dates, ordered follow-up dates).
    /// </summary>
    public static class ValidationHelper
    {
        public static bool IsValidName(string? name, int maxLen, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "⚠ Lead / Customer Name is required.";
                return false;
            }

            var trimmed = name.Trim();
            if (trimmed.Length < 2)
            {
                errorMessage = "⚠ Name must be at least 2 characters long.";
                return false;
            }

            if (trimmed.Length > maxLen)
            {
                errorMessage = $"⚠ Name cannot exceed {maxLen} characters.";
                return false;
            }

            if (!Regex.IsMatch(trimmed, @"^[\p{L}\p{M}'\.\-\s]+$"))
            {
                errorMessage = "⚠ Name should contain letters only (spaces, hyphens, and periods allowed).";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public static bool IsValidPhoneNumber(string? phone, bool isRequired, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                if (isRequired)
                {
                    errorMessage = "⚠ Phone number is required.";
                    return false;
                }
                errorMessage = string.Empty;
                return true;
            }

            var trimmed = phone.Trim();

            // Reject any alphabet characters
            if (Regex.IsMatch(trimmed, @"[a-zA-Z]"))
            {
                errorMessage = "⚠ Phone number must contain digits only.";
                return false;
            }

            // Extract digits only
            var digitsOnly = Regex.Replace(trimmed, @"\D", "");
            if (digitsOnly.Length < 7 || digitsOnly.Length > 15)
            {
                errorMessage = "⚠ Phone number must be between 7 and 15 digits.";
                return false;
            }

            // Check permitted structure: optional leading +, digits, spaces, hyphens, parentheses
            if (!Regex.IsMatch(trimmed, @"^\+?[\d\s\-\(\)\.]+$"))
            {
                errorMessage = "⚠ Phone number contains invalid characters. Digits, +, -, and spaces only.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public static bool IsValidEmail(string? email, bool isRequired, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                if (isRequired)
                {
                    errorMessage = "⚠ Email address is required.";
                    return false;
                }
                errorMessage = string.Empty;
                return true;
            }

            var trimmed = email.Trim();
            if (trimmed.Length > 120)
            {
                errorMessage = "⚠ Email address cannot exceed 120 characters.";
                return false;
            }

            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(trimmed, emailPattern, RegexOptions.IgnoreCase))
            {
                errorMessage = "⚠ Please enter a valid email address (e.g. name@example.com).";
                return false;
            }

            try
            {
                var addr = new MailAddress(trimmed);
                if (addr.Address != trimmed)
                {
                    errorMessage = "⚠ Please enter a valid email address.";
                    return false;
                }
            }
            catch
            {
                errorMessage = "⚠ Please enter a valid email address.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public static bool IsValidPrice(string? priceText, bool isRequired, out decimal? price, out string errorMessage)
        {
            price = null;
            if (string.IsNullOrWhiteSpace(priceText))
            {
                if (isRequired)
                {
                    errorMessage = "⚠ Price amount is required.";
                    return false;
                }
                errorMessage = string.Empty;
                return true;
            }

            var clean = priceText.Trim().Replace("₱", "").Replace("$", "").Replace(",", "").Trim();
            if (!decimal.TryParse(clean, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var val))
            {
                errorMessage = "⚠ Price must be a valid numeric amount.";
                return false;
            }

            if (val < 0)
            {
                errorMessage = "⚠ Price cannot be negative.";
                return false;
            }

            if (val > 10_000_000)
            {
                errorMessage = "⚠ Price amount exceeds maximum limit of ₱10,000,000.";
                return false;
            }

            price = val;
            errorMessage = string.Empty;
            return true;
        }

        public static bool IsValidTextLength(string? text, string fieldName, int maxLen, bool isRequired, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                if (isRequired)
                {
                    errorMessage = $"⚠ {fieldName} is required.";
                    return false;
                }
                errorMessage = string.Empty;
                return true;
            }

            var trimmed = text.Trim();
            if (trimmed.Length > maxLen)
            {
                errorMessage = $"⚠ {fieldName} cannot exceed {maxLen} characters (currently {trimmed.Length}).";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public static bool IsValidDateNotPast(DateTime date, string fieldName, out string errorMessage)
        {
            if (date.Date < DateTime.Today)
            {
                errorMessage = $"⚠ {fieldName} cannot be in the past.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public static bool IsValidDateOrder(DateTime preferredDate, DateTime? followUpDate, out string errorMessage)
        {
            if (followUpDate.HasValue && followUpDate.Value.Date < preferredDate.Date)
            {
                errorMessage = "⚠ Follow-up date must be on or after the preferred booking date.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
