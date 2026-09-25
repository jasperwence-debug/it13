namespace App.Domain.Enums
{
    /// <summary>
    /// Lifecycle status definitions for customer inquiries/leads.
    /// </summary>
    public enum LeadStatus
    {
        New,
        Contacted,
        Quoted,
        Won,
        Lost,
        Converted
    }
}
