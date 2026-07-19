// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator API — Human Approval Queue
// Human Approval Layer: deterministic, thread-safe store for pending write
// actions. Never auto-expires, never auto-approves.
// ══════════════════════════════════════════════════════════════════════════════

using System.Collections.Concurrent;
using META7.Operator.Contracts.Approval;

namespace META7.Operator.Api.Approval;

/// <summary>
/// Thread-safe in-memory queue of write-capable actions awaiting human decision.
/// <para>
/// Guarantees:
/// <list type="bullet">
///   <item>No action is ever auto-approved or auto-expired.</item>
///   <item>All state transitions require an explicit human-triggered call.</item>
///   <item>Approved and rejected records are retained for audit purposes.</item>
/// </list>
/// </para>
/// </summary>
public class HumanApprovalQueue
{
    private readonly ConcurrentDictionary<string, HumanApprovalRecord> _store = new();

    // ── Writes ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Add a new approval record to the queue.
    /// Throws <see cref="ArgumentException"/> if a record with the same
    /// <see cref="HumanApprovalRecord.RequestId"/> already exists.
    /// </summary>
    public void AddRequest(HumanApprovalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!_store.TryAdd(record.RequestId, record))
            throw new ArgumentException(
                $"A request with ID {record.RequestId} already exists in the queue.",
                nameof(record));
    }

    /// <summary>
    /// Approve the request identified by <paramref name="requestId"/>.
    /// Returns <c>false</c> if no matching Pending record exists.
    /// </summary>
    /// <param name="requestId">The request to approve.</param>
    /// <param name="approvedBy">The human operator performing the approval.</param>
    /// <param name="approvedAt">Override approval timestamp (defaults to UtcNow).</param>
    public bool ApproveRequest(string requestId, string approvedBy, DateTime? approvedAt = null)
    {
        if (!_store.TryGetValue(requestId, out var record))
            return false;
        if (record.Status != HumanApprovalStatus.Pending)
            return false;

        record.Approve(approvedBy, approvedAt);
        return true;
    }

    /// <summary>
    /// Reject the request identified by <paramref name="requestId"/>.
    /// Returns <c>false</c> if no matching Pending record exists.
    /// </summary>
    /// <param name="requestId">The request to reject.</param>
    /// <param name="reason">Optional reason for rejection.</param>
    /// <param name="rejectedAt">Override rejection timestamp (defaults to UtcNow).</param>
    public bool RejectRequest(string requestId, string? reason = null, DateTime? rejectedAt = null)
    {
        if (!_store.TryGetValue(requestId, out var record))
            return false;
        if (record.Status != HumanApprovalStatus.Pending)
            return false;

        record.Reject(reason, rejectedAt);
        return true;
    }

    // ── Reads ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all records currently in <see cref="HumanApprovalStatus.Pending"/> state,
    /// ordered by <see cref="HumanApprovalRecord.CreatedAt"/> ascending (oldest first).
    /// </summary>
    public IReadOnlyList<HumanApprovalRecord> GetPendingRequests() =>
        _store.Values
              .Where(r => r.Status == HumanApprovalStatus.Pending)
              .OrderBy(r => r.CreatedAt)
              .ToList();

    /// <summary>
    /// Returns the record for <paramref name="requestId"/>, or <c>null</c> if not found.
    /// </summary>
    public HumanApprovalRecord? GetById(string requestId) =>
        _store.TryGetValue(requestId, out var r) ? r : null;

    /// <summary>Total number of records (all statuses) held in the queue.</summary>
    public int Count => _store.Count;
}
