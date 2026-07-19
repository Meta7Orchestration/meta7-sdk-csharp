using META7.Operator.Contracts.Revenue;

namespace META7.Operator.Contracts;

public enum OperatorActionType
{
    Analyze,
    Plan,
    Report,
    SubmitLeadForm,
    RequestCallback,
    TriggerWebhook,
    CreateSupportTicket,
    RegisterInterest
}

public static class OperatorActionTypeExtensions
{
    public static bool IsRevenueWriteAction(this OperatorActionType actionType) => actionType is
        OperatorActionType.SubmitLeadForm or
        OperatorActionType.RequestCallback or
        OperatorActionType.TriggerWebhook or
        OperatorActionType.CreateSupportTicket or
        OperatorActionType.RegisterInterest;

    public static RevenueActionType ToRevenueActionType(this OperatorActionType actionType) => actionType switch
    {
        OperatorActionType.SubmitLeadForm => RevenueActionType.SubmitLeadForm,
        OperatorActionType.RequestCallback => RevenueActionType.RequestCallback,
        OperatorActionType.TriggerWebhook => RevenueActionType.TriggerWebhook,
        OperatorActionType.CreateSupportTicket => RevenueActionType.CreateSupportTicket,
        OperatorActionType.RegisterInterest => RevenueActionType.RegisterInterest,
        _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, "Action is not a revenue write action.")
    };
}
