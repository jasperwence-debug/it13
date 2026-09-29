using System.ComponentModel;
using System.Globalization;

namespace CRM.domain.Enums
{
    /// <summary>
    /// Allows enums to be bound from query strings using their names
    /// (e.g., ?status=New) instead of only numeric values.
    /// </summary>
    public class StringEnumConverter<TEnum> : TypeConverter where TEnum : struct, Enum
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(
            ITypeDescriptorContext? context,
            CultureInfo? culture,
            object value)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                if (Enum.TryParse<TEnum>(s, ignoreCase: true, out var result))
                    return result;

                // Also allow numeric strings ("0", "5", etc.)
                if (int.TryParse(s, out var number))
                    return (TEnum)Enum.ToObject(typeof(TEnum), number);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}