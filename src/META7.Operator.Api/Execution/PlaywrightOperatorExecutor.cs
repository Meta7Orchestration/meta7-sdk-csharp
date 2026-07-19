using System.Security;
using Microsoft.Extensions.Options;

namespace META7.Operator.Api.Execution;

public interface IOperatorExecutor
{
    Task<OperatorResult> ExecuteAsync(OperatorDirective directive, CancellationToken cancellationToken);
}

public sealed class PlaywrightOperatorExecutor : IOperatorExecutor
{
    private readonly IBrowserBootstrap _browserBootstrap;
    private readonly OperatorExecutionOptions _options;

    public PlaywrightOperatorExecutor(IBrowserBootstrap browserBootstrap, IOptions<OperatorExecutionOptions> options)
    {
        _browserBootstrap = browserBootstrap;
        _options = options.Value;
    }

    public async Task<OperatorResult> ExecuteAsync(OperatorDirective directive, CancellationToken cancellationToken)
    {
        var requestedTimeout = directive.TimeoutMs > 0 ? directive.TimeoutMs : _options.DefaultTimeoutMs;
        var timeoutMs = Math.Min(requestedTimeout, _options.MaxTimeoutMs);
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            if (!string.IsNullOrWhiteSpace(directive.Url))
            {
                if (!Uri.TryCreate(directive.Url, UriKind.Absolute, out var uri) || !_options.IsAllowedDomain(uri))
                {
                    return new OperatorResult(false, "Navigation blocked by domain allowlist.", new(), []);
                }
            }

            await using var session = await _browserBootstrap.CreateSessionAsync(_options, linkedCts.Token);
            if (!string.IsNullOrWhiteSpace(directive.Url))
            {
                await session.NavigateAsync(directive.Url, linkedCts.Token);
            }

            return directive.ActionType switch
            {
                OperatorActionType.Navigate => NavigateResult(directive),
                OperatorActionType.ReadPage => await ReadPageResultAsync(session, linkedCts.Token),
                OperatorActionType.ExtractStructuredData => await ExtractStructuredDataResultAsync(session, directive, linkedCts.Token),
                OperatorActionType.WaitForElement => await WaitForElementResultAsync(session, directive, timeoutMs, linkedCts.Token),
                OperatorActionType.TakeScreenshot => await TakeScreenshotResultAsync(session, linkedCts.Token),
                _ => new OperatorResult(false, "Unsupported operator action.", new(), [])
            };
        }
        catch (OperationCanceledException)
        {
            return new OperatorResult(false, "Directive timed out.", new(), []);
        }
        catch (SecurityException ex)
        {
            return new OperatorResult(false, ex.Message, new(), []);
        }
    }

    private static OperatorResult NavigateResult(OperatorDirective directive)
    {
        return new OperatorResult(
            true,
            "Navigation completed.",
            new Dictionary<string, object?> { ["url"] = directive.Url },
            []);
    }

    private static async Task<OperatorResult> ReadPageResultAsync(IReadOnlyBrowserSession session, CancellationToken cancellationToken)
    {
        var page = await session.ReadPageAsync(cancellationToken);
        return new OperatorResult(
            true,
            "Page read completed.",
            new Dictionary<string, object?>
            {
                ["url"] = page.Url,
                ["title"] = page.Title,
                ["textContent"] = page.TextContent
            },
            []);
    }

    private static async Task<OperatorResult> ExtractStructuredDataResultAsync(
        IReadOnlyBrowserSession session,
        OperatorDirective directive,
        CancellationToken cancellationToken)
    {
        if (directive.Selectors is null || directive.Selectors.Count == 0)
        {
            return new OperatorResult(false, "Selectors are required for structured extraction.", new(), []);
        }

        var data = await session.ExtractStructuredDataAsync(directive.Selectors, cancellationToken);
        return new OperatorResult(
            true,
            "Structured data extraction completed.",
            new Dictionary<string, object?> { ["data"] = data },
            []);
    }

    private static async Task<OperatorResult> WaitForElementResultAsync(
        IReadOnlyBrowserSession session,
        OperatorDirective directive,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(directive.Selector))
        {
            return new OperatorResult(false, "Selector is required for WaitForElement.", new(), []);
        }

        var found = await session.WaitForElementAsync(directive.Selector, TimeSpan.FromMilliseconds(timeoutMs), cancellationToken);
        return new OperatorResult(
            found,
            found ? "Element became visible." : "Element was not visible before timeout.",
            new Dictionary<string, object?> { ["selector"] = directive.Selector, ["found"] = found },
            []);
    }

    private static async Task<OperatorResult> TakeScreenshotResultAsync(IReadOnlyBrowserSession session, CancellationToken cancellationToken)
    {
        var base64 = await session.TakeScreenshotBase64Async(cancellationToken);
        return new OperatorResult(
            true,
            "Screenshot captured.",
            new(),
            [new OperatorArtifact("page.png", "image/png", base64)]);
    }
}
