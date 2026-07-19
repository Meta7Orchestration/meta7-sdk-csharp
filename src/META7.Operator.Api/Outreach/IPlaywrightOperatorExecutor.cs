using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace META7.Operator.Api.Outreach;

public interface IPlaywrightOperatorExecutor
{
    Task<ReadOnlyPageSnapshot> CaptureReadOnlyAsync(
        Uri targetUri,
        IReadOnlyList<ReadOnlyBrowserActionType> actions,
        CancellationToken cancellationToken = default);
}
