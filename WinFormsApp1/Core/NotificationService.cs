using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.WinForms.Core
{
    public enum NotificationType
    {
        ApprovalRequired,
        PaymentPending,
        ServiceDispatched,
        SystemAlert
    }

    public class NotificationItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ActionLabel { get; set; } = "View Details";
        public string TargetType { get; set; } = string.Empty;
        public object? Payload { get; set; }
    }

    /// <summary>
    /// Evaluates operational CRM state and generates real-time role-aware notifications and approval queues.
    /// </summary>
    public class NotificationService
    {
        private readonly ApiClient _api = new();

        public async Task<List<NotificationItem>> GetNotificationsForCurrentRoleAsync()
        {
            var list = new List<NotificationItem>();
            var role = SessionManager.CurrentUser?.Role ?? Roles.SalesStaff;

            try
            {
                var workOrders = await _api.GetWorkOrdersAsync();
                if (workOrders == null || workOrders.Count == 0)
                {
                    return list;
                }

                if (role == Roles.Manager)
                {
                    // 1. Manager Approval Queue: Newly submitted bookings in 'Requested' status
                    var requestedOrders = workOrders
                        .Where(w => string.Equals(w.Status, "Requested", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(w => w.ServiceRequestId)
                        .ToList();

                    foreach (var wo in requestedOrders)
                    {
                        list.Add(new NotificationItem
                        {
                            Type = NotificationType.ApprovalRequired,
                            Title = $"Booking Approval Needed (WO-{wo.ServiceRequestId:D4})",
                            Message = $"{wo.CustomerName} requested '{wo.ServiceType}' for {wo.PreferredDate:MMM dd, yyyy}. Awaiting Manager approval & technician assignment.",
                            Timestamp = wo.CreatedAt > DateTime.MinValue ? wo.CreatedAt : DateTime.Now,
                            ActionLabel = "Review & Dispatch",
                            TargetType = "dispatch",
                            Payload = wo
                        });
                    }

                    // 2. Urgent: Unassigned services scheduled within 48 hours
                    var urgentUnassigned = workOrders
                        .Where(w => (string.Equals(w.Status, "Scheduled", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(w.Status, "Requested", StringComparison.OrdinalIgnoreCase)) &&
                                    (string.IsNullOrWhiteSpace(w.AssignedStaff) || w.AssignedStaff == "Unassigned") &&
                                    w.PreferredDate.Date >= DateTime.Today &&
                                    w.PreferredDate.Date <= DateTime.Today.AddDays(2))
                        .ToList();

                    foreach (var wo in urgentUnassigned)
                    {
                        if (!list.Any(n => n.Payload is WorkOrderDto existing && existing.ServiceRequestId == wo.ServiceRequestId))
                        {
                            list.Add(new NotificationItem
                            {
                                Type = NotificationType.ApprovalRequired,
                                Title = $"Urgent: Unassigned Crew (WO-{wo.ServiceRequestId:D4})",
                                Message = $"Job for {wo.CustomerName} is scheduled within 48 hours on {wo.PreferredDate:MMM dd} with no crew assigned.",
                                Timestamp = DateTime.Now,
                                ActionLabel = "Assign Crew",
                                TargetType = "dispatch",
                                Payload = wo
                            });
                        }
                    }
                }
                else if (role == Roles.Admin)
                {
                    // Admin Financial Approvals: Completed jobs pending payment settlement
                    var completedUnsettled = workOrders
                        .Where(w => string.Equals(w.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                                    (w.ActualPrice.HasValue || w.QuotedPrice.HasValue))
                        .Take(15)
                        .ToList();

                    foreach (var wo in completedUnsettled)
                    {
                        decimal amount = wo.ActualPrice ?? wo.QuotedPrice ?? 0m;
                        list.Add(new NotificationItem
                        {
                            Type = NotificationType.PaymentPending,
                            Title = $"Pending Settlement (INV-{wo.ServiceRequestId:D4})",
                            Message = $"{wo.CustomerName} • '{wo.ServiceType}' completed. ₱{amount:N2} awaiting payment confirmation.",
                            Timestamp = wo.CreatedAt > DateTime.MinValue ? wo.CreatedAt : DateTime.Now,
                            ActionLabel = "Settle Invoice",
                            TargetType = "financial",
                            Payload = wo
                        });
                    }
                }
                else if (role == Roles.SalesStaff)
                {
                    // Sales Staff Updates: Bookings that were approved & dispatched by the Manager
                    var dispatchedOrders = workOrders
                        .Where(w => string.Equals(w.Status, "Scheduled", StringComparison.OrdinalIgnoreCase) &&
                                    !string.IsNullOrWhiteSpace(w.AssignedStaff) &&
                                    w.AssignedStaff != "Unassigned")
                        .OrderByDescending(w => w.ServiceRequestId)
                        .Take(10)
                        .ToList();

                    foreach (var wo in dispatchedOrders)
                    {
                        list.Add(new NotificationItem
                        {
                            Type = NotificationType.ServiceDispatched,
                            Title = $"Booking Confirmed: WO-{wo.ServiceRequestId:D4}",
                            Message = $"Manager approved & dispatched '{wo.ServiceType}' for {wo.CustomerName} to technician {wo.AssignedStaff} ({wo.PreferredDate:MMM dd}).",
                            Timestamp = wo.CreatedAt > DateTime.MinValue ? wo.CreatedAt : DateTime.Now,
                            ActionLabel = "View Schedule",
                            TargetType = "scheduling",
                            Payload = wo
                        });
                    }
                }
                else if (role == Roles.SuperAdmin)
                {
                    // Super Admin Platform & Tenant Governance: Monitor tenant subscriptions and renewals
                    var subs = await _api.GetSubscriptionsAsync();
                    if (subs != null)
                    {
                        var expiringOrSuspended = subs
                            .Where(s => s.EndDate <= DateTime.Today.AddDays(7) || string.Equals(s.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        foreach (var sub in expiringOrSuspended)
                        {
                            bool isSuspended = string.Equals(sub.Status, "Suspended", StringComparison.OrdinalIgnoreCase);
                            list.Add(new NotificationItem
                            {
                                Type = NotificationType.SystemAlert,
                                Title = isSuspended ? $"Suspended Tenant: {sub.CompanyName}" : $"Subscription Expiring: {sub.CompanyName}",
                                Message = isSuspended 
                                    ? $"Tenant {sub.CompanyName} ({sub.Tier}) subscription is currently suspended." 
                                    : $"Tenant {sub.CompanyName} ({sub.Tier}) plan ends on {sub.EndDate:MMM dd, yyyy}. Renewal required.",
                                Timestamp = DateTime.Now,
                                ActionLabel = "Manage Subscription",
                                TargetType = "subscription",
                                Payload = sub
                            });
                        }
                    }
                }
            }
            catch
            {
                // Non-critical background telemetry
            }

            return list;
        }
    }
}
