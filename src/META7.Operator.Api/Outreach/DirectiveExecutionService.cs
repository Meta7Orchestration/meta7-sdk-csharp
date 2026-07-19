using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using META7.Operator.Contracts.Outreach;

namespace META7.Operator.Api.Outreach;

public sealed class DirectiveExecutionService
{
    private static readonly Uri PolicyLocalContextUri = new("https://policy.local/outreach");

    private readonly ISafeLockStateProvider _safeLockStateProvider;
    private readonly IOutreachPolicyGate _policyGate;
    private readonly OutreachSignalDetector _signalDetector;
    private readonly OutreachContextAnalyzer _contextAnalyzer;
    private readonly OutreachValuePropositionEngine _valuePropositionEngine;
    private readonly OutreachSuggestionFormatter _suggestionFormatter;

    public DirectiveExecutionService(
        ISafeLockStateProvider safeLockStateProvider,
        IOutreachPolicyGate policyGate,
        OutreachSignalDetector signalDetector,
        OutreachContextAnalyzer contextAnalyzer,
        OutreachValuePropositionEngine valuePropositionEngine,
        OutreachSuggestionFormatter suggestionFormatter)
    {
        _safeLockStateProvider = safeLockStateProvider ?? throw new ArgumentNullException(nameof(safeLockStateProvider));
        _policyGate = policyGate ?? throw new ArgumentNullException(nameof(policyGate));
        _signalDetector = signalDetector ?? throw new ArgumentNullException(nameof(signalDetector));
        _contextAnalyzer = contextAnalyzer ?? throw new ArgumentNullException(nameof(contextAnalyzer));
        _valuePropositionEngine = valuePropositionEngine ?? throw new ArgumentNullException(nameof(valuePropositionEngine));
        _suggestionFormatter = suggestionFormatter ?? throw new ArgumentNullException(nameof(suggestionFormatter));
    }

    public async Task<object> ExecuteAsync(
        OperatorActionType actionType,
        object payload,
        CancellationToken cancellationToken = default)
    {
        if (_safeLockStateProvider.IsSafeLockActive)
        {
            throw new InvalidOperationException("SAFE_LOCK active - outreach actions blocked.");
        }

        switch (actionType)
        {
            case OperatorActionType.DetectOutreachSignals:
            {
                var targetUrl = payload as string
                    ?? throw new ArgumentException("DetectOutreachSignals payload must be target URL string.", nameof(payload));

                if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri))
                {
                    throw new ArgumentException("Target URL is invalid.", nameof(payload));
                }

                EnforcePolicy(actionType, uri);
                return await _signalDetector.DetectAsync(targetUrl, cancellationToken).ConfigureAwait(false);
            }
            case OperatorActionType.AnalyzeOutreachContext:
            {
                var signals = ReadSignals(payload);
                var targetUri = ResolveSignalSourceUri(signals);
                EnforcePolicy(actionType, targetUri);
                return _contextAnalyzer.Analyze(signals);
            }
            case OperatorActionType.GenerateOutreachSuggestion:
            {
                var signals = ReadSignals(payload);
                var targetUri = ResolveSignalSourceUri(signals);
                EnforcePolicy(actionType, targetUri);
                var context = _contextAnalyzer.Analyze(signals);
                var valuePropositions = _valuePropositionEngine.Generate(context);
                return _suggestionFormatter.Format(context, valuePropositions);
            }
            default:
                throw new NotSupportedException($"Unknown operator action: {actionType}");
        }
    }

    private void EnforcePolicy(OperatorActionType actionType, Uri targetUri)
    {
        if (!_policyGate.CanExecute(actionType, targetUri))
        {
            throw new UnauthorizedAccessException($"Policy gate blocked action '{actionType}' for '{targetUri.Host}'.");
        }
    }

    private static IReadOnlyList<OutreachSignal> ReadSignals(object payload) =>
        payload as IReadOnlyList<OutreachSignal>
        ?? throw new ArgumentException("Outreach action payload must be IReadOnlyList<OutreachSignal>.", nameof(payload));

    private static Uri ResolveSignalSourceUri(IReadOnlyList<OutreachSignal> signals) =>
        signals.Count > 0 && Uri.TryCreate(signals[0].SourceUrl, UriKind.Absolute, out var sourceUri)
            ? sourceUri
            : PolicyLocalContextUri;
}
