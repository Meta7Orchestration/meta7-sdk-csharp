using System;

namespace META7.Operator.Api.Outreach;

public interface IOutreachPolicyGate
{
    bool CanExecute(OperatorActionType actionType, Uri targetUri);
}
