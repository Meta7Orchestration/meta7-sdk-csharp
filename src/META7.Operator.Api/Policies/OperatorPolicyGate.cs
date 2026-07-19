namespace META7.Operator.Api.Policies;

using META7.Operator.Contracts;

/// <summary>Result of evaluating an <see cref="OperatorDirective"/> against policy rules.</summary>
internal sealed class PolicyEvaluation
{
    public bool IsAllowed { get; private init; }
    public OperatorExecutionStatus RejectionStatus { get; private init; }
    public string Reason { get; private init; } = string.Empty;

    public static PolicyEvaluation Allow() =>
        new() { IsAllowed = true };

    public static PolicyEvaluation Reject(OperatorExecutionStatus status, string reason) =>
        new() { IsAllowed = false, RejectionStatus = status, Reason = reason };
}

/// <summary>
/// Evaluates an <see cref="OperatorDirective"/> against operator policy rules:
/// <list type="bullet">
///   <item>Directive must not be expired.</item>
///   <item>The host portion of <see cref="OperatorDirective.TargetUrl"/> must appear in <see cref="OperatorDirective.AllowedDomains"/>.</item>
/// </list>
/// </summary>
public sealed class OperatorPolicyGate
{
    /// <summary>Evaluates the directive and returns a <see cref="PolicyEvaluation"/>.</summary>
    internal PolicyEvaluation Evaluate(OperatorDirective directive)
    {
        if (directive.ExpiresAt <= DateTime.UtcNow)
        {
            return PolicyEvaluation.Reject(
                OperatorExecutionStatus.Expired,
                $"Directive '{directive.Id}' expired at {directive.ExpiresAt:O}.");
        }

        if (!string.IsNullOrWhiteSpace(directive.TargetUrl) && directive.AllowedDomains.Count > 0)
        {
            if (!Uri.TryCreate(directive.TargetUrl, UriKind.Absolute, out var uri))
            {
                return PolicyEvaluation.Reject(
                    OperatorExecutionStatus.PolicyViolation,
                    $"TargetUrl '{directive.TargetUrl}' is not a valid absolute URI.");
            }

            var host = uri.Host;
            if (!directive.AllowedDomains.Contains(host, StringComparer.OrdinalIgnoreCase))
            {
                return PolicyEvaluation.Reject(
                    OperatorExecutionStatus.PolicyViolation,
                    $"Domain '{host}' is not in the allowlist.");
            }
        }

        return PolicyEvaluation.Allow();
    }
}
