using CRM.domain.Enums;

namespace CRM.api.Contracts.FollowUps
{
    public record CompleteFollowUpRequest(FollowUpStatus Status);
}