using System;
using System.Globalization;
using System.Linq;

namespace App.Domain.Common
{
    /// <summary>
    /// Utility for normalizing customer and lead names.
    /// Enforces:
    ///   - Whitespace trimming and Title Case normalization on save.
    ///   - Standardized formatting of read-only FullName (FirstName MiddleName LastName Suffix).
    ///   - Tokenization of legacy single full-name strings for database migration.
    ///   - 50-character maximum length bounds per field.
    /// </summary>
    public static class NameNormalizer
    {
        public const int MaxFieldLength = 50;

        /// <summary>
        /// Trims whitespace and converts string to Title Case.
        /// </summary>
        public static string Normalize(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return string.Empty;
            var trimmed = val.Trim();
            if (trimmed.Length > MaxFieldLength)
            {
                trimmed = trimmed.Substring(0, MaxFieldLength).Trim();
            }
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(trimmed.ToLowerInvariant());
        }

        /// <summary>
        /// Normalizes optional suffix (e.g. Jr., Sr., II, III, IV) with appropriate casing.
        /// </summary>
        public static string? NormalizeSuffix(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            var trimmed = val.Trim();
            if (trimmed.Length > MaxFieldLength)
            {
                trimmed = trimmed.Substring(0, MaxFieldLength).Trim();
            }
            var upper = trimmed.ToUpperInvariant().Replace(".", "");
            if (upper is "JR" or "SR")
            {
                return upper == "JR" ? "Jr." : "Sr.";
            }
            if (upper is "II" or "III" or "IV" or "V" or "VI" or "VII" or "VIII" or "IX" or "X")
            {
                return upper;
            }
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            var result = textInfo.ToTitleCase(trimmed.ToLowerInvariant());
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        /// <summary>
        /// Migrates a legacy single string by splitting on spaces:
        ///   - First token -> FirstName
        ///   - Last token  -> LastName
        ///   - Any middle tokens -> MiddleName
        /// All parts trimmed, normalized to Title Case, and capped at 50 chars.
        /// </summary>
        public static (string FirstName, string? MiddleName, string LastName) SplitSingleString(string? rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return (string.Empty, null, string.Empty);

            var tokens = rawName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                return (string.Empty, null, string.Empty);

            if (tokens.Length == 1)
            {
                var single = Normalize(tokens[0]);
                return (single, null, single);
            }

            if (tokens.Length == 2)
            {
                return (Normalize(tokens[0]), null, Normalize(tokens[1]));
            }

            var firstName = Normalize(tokens[0]);
            var lastName = Normalize(tokens[^1]);
            var middleRaw = string.Join(" ", tokens.Skip(1).Take(tokens.Length - 2));
            var middle = Normalize(middleRaw);

            return (firstName, string.IsNullOrWhiteSpace(middle) ? null : middle, lastName);
        }

        /// <summary>
        /// Formats read-only FullName in the strict order:
        /// FirstName MiddleName LastName Suffix
        /// </summary>
        public static string FormatFullName(string? firstName, string? middleName, string? lastName, string? suffix)
        {
            var parts = new[] { firstName, middleName, lastName, suffix };
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));
        }
    }
}
