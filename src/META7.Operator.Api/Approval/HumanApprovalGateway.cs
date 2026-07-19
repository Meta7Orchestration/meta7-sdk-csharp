// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator API — Human Approval Gateway
// Human Approval Layer: mandatory interception point for every write-capable
// action. Actions are packaged and queued; none are executed until approved.
// ══════════════════════════════════════════════════════════════════════════════

using META7.Operator.Contracts.Approval;

namespace META7.Operator.Api.Approval;

/// <summary>
/// Gateway that intercepts ALL write-capable <see cref="OperatorActionType"/>s
/// before execution and holds them in <see cref="HumanApprovalQueue"/> until a
/// human operator explicitly approves or rejects them.
/// <para>
/// Safety invariants:
/// <list type="bullet">
///   <item>No write action bypasses this gateway.</item>
///   <item>Intercept() never executes the action — it only queues it.</item>
///   <item>The gateway never self-approves or auto-expires requests.</item>
/// </list>
/// </para>
/// </summary>
public class HumanApprovalGateway
{
    private readonly HumanApprovalQueue _queue;

    /// <summary>Write-capable action types that always require human approval.</summary>
    public static readonly IReadOnlySet<OperatorActionType> ApprovalRequiredActions =
        new HashSet<OperatorActionType>
        {
            OperatorActionType.SubmitLeadForm,
            OperatorActionType.RequestCallback,
            OperatorActionType.TriggerWebhook,
            OperatorActionType.CreateSupportTicket,
            OperatorActionType.RegisterInterest,
        };

    /// <summary>Domain allowlist. When non-empty, only domains in the set are accepted.</summary>
    private readonly IReadOnlySet<string>? _allowedDomains;

    public HumanApprovalGateway(
        HumanApprovalQueue queue,
        IReadOnlySet<string>? allowedDomains = null)
    {
        _queue          = queue ?? throw new ArgumentNullException(nameof(queue));
        _allowedDomains = allowedDomains;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when <paramref name="actionType"/> is write-capable
    /// and must be intercepted by this gateway.
    /// </summary>
    public bool IsWriteAction(OperatorActionType actionType) =>
        ApprovalRequiredActions.Contains(actionType);

    /// <summary>
    /// Intercept a write-capable action, package it as an
    /// <see cref="ApprovalRequest"/>, and store it in the approval queue.
    /// </summary>
    /// <param name="directiveId">ID of the originating directive.</param>
    /// <param name="actionType">The write-capable action to intercept.</param>
    /// <param name="payload">Serialised action payload.</param>
    /// <param name="domain">
    /// Optional originating domain for allowlist enforcement.
    /// If an allowlist is configured and this domain is absent, the interception
    /// is rejected with an <see cref="UnauthorizedAccessException"/>.
    /// </param>
    /// <param name="requestId">
    /// Optional deterministic request ID (useful in tests).
    /// Defaults to a new GUID.
    /// </param>
    /// <param name="createdAt">
    /// Optional creation timestamp (useful in tests).
    /// Defaults to <see cref="DateTime.UtcNow"/>.
    /// </param>
    /// <returns>
    /// An <see cref="ApprovalRequest"/> with status implicitly
    /// <see cref="ApprovalStatus.Pending"/>. The action is NOT executed.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="actionType"/> is NOT a write-capable action.
    /// Read-only actions must not pass through this gateway.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when a domain allowlist is configured and the supplied
    /// <paramref name="domain"/> is not in it.
    /// </exception>
    public ApprovalRequest Intercept(
        string             directiveId,
        OperatorActionType actionType,
        string             payload,
        string?            domain    = null,
        string?            requestId = null,
        DateTime?          createdAt = null)
    {
        if (!IsWriteAction(actionType))
            throw new ArgumentException(
                $"{actionType} is not a write-capable action and must not pass through HumanApprovalGateway.",
                nameof(actionType));

        if (_allowedDomains is { Count: > 0 } && domain is not null &&
            !_allowedDomains.Contains(domain))
        {
            throw new UnauthorizedAccessException(
                $"Domain '{domain}' is not on the approved allowlist.");
        }

        var id     = requestId ?? Guid.NewGuid().ToString();
        var now    = createdAt ?? DateTime.UtcNow;
        var record = new HumanApprovalRecord(id, directiveId, actionType, payload, now, domain);

        _queue.AddRequest(record);

        return new ApprovalRequest(
            RequestId:   id,
            DirectiveId: directiveId,
            ActionType:  actionType,
            Payload:     payload,
            CreatedAt:   now,
            Domain:      domain);
    }
}
