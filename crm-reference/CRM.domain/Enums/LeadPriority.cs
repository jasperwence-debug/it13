using System.ComponentModel;

namespace CRM.domain.Enums
{
    [TypeConverter(typeof(StringEnumConverter<LeadPriority>))]
    public enum LeadPriority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
}