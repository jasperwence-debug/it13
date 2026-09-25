using System;
using System.Drawing;
using App.Domain.Enums;

namespace App.WinForms.Core
{
    /// <summary>
    /// Color-coding, formatting, and display helpers for Lead statuses.
    /// </summary>
    public static class LeadStatusHelper
    {
        public static Color GetStatusColor(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return Color.FromArgb(100, 116, 139); // Slate

            if (!Enum.TryParse<LeadStatus>(status.Trim(), true, out var leadStatus))
                return Color.FromArgb(100, 116, 139);

            return leadStatus switch
            {
                LeadStatus.New       => Color.FromArgb(37, 99, 235),  // Blue #2563EB
                LeadStatus.Contacted => Color.FromArgb(124, 58, 237), // Purple #7C3AED
                LeadStatus.Quoted    => Color.FromArgb(217, 119, 6),  // Amber #D97706
                LeadStatus.Won       => Color.FromArgb(22, 163, 74),  // Green #16A34A
                LeadStatus.Lost      => Color.FromArgb(220, 38, 38),  // Red #DC2626
                LeadStatus.Converted => Color.FromArgb(71, 85, 105),  // Slate #475569
                _                    => Color.FromArgb(100, 116, 139)
            };
        }

        public static Color GetStatusBgColor(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return Color.FromArgb(241, 245, 249);

            if (!Enum.TryParse<LeadStatus>(status.Trim(), true, out var leadStatus))
                return Color.FromArgb(241, 245, 249);

            return leadStatus switch
            {
                LeadStatus.New       => Color.FromArgb(239, 246, 255), // Light blue
                LeadStatus.Contacted => Color.FromArgb(245, 243, 255), // Light purple
                LeadStatus.Quoted    => Color.FromArgb(254, 243, 199), // Light amber
                LeadStatus.Won       => Color.FromArgb(240, 253, 244), // Light green
                LeadStatus.Lost      => Color.FromArgb(254, 242, 242), // Light red
                LeadStatus.Converted => Color.FromArgb(241, 245, 249), // Light slate
                _                    => Color.FromArgb(241, 245, 249)
            };
        }
    }
}
