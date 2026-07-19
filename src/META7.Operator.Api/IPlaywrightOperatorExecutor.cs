// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// IPlaywrightOperatorExecutor: Read-only browser abstraction
// No write browser actions are exposed through this interface
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Api;

/// <summary>
/// Abstraction over Playwright browser execution in read-only mode.
/// Only navigation and content extraction are permitted — no input, click, or submit methods.
/// </summary>
public interface IPlaywrightOperatorExecutor
{
    /// <summary>Navigate to a URL (read-only — no form submissions, no logins).</summary>
    Task NavigateAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Get the text content of all elements matching a CSS selector.</summary>
    Task<IReadOnlyList<string>> GetAllTextAsync(string cssSelector, CancellationToken cancellationToken = default);

    /// <summary>Get the text content of the first element matching a CSS selector.</summary>
    Task<string?> GetTextContentAsync(string cssSelector, CancellationToken cancellationToken = default);

    /// <summary>Get an attribute value from the first element matching a CSS selector.</summary>
    Task<string?> GetAttributeAsync(string cssSelector, string attribute, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the current page is accessible (i.e. not a login wall or 404).</summary>
    Task<bool> IsPageAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns true if the current page shows a login-required gate.</summary>
    Task<bool> IsLoginRequiredAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the current page URL after navigation (to detect redirects).</summary>
    Task<string> GetCurrentUrlAsync(CancellationToken cancellationToken = default);
}
