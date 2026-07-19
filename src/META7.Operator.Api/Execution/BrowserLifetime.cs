using System.Security;
using Microsoft.Playwright;

namespace META7.Operator.Api.Execution;

public interface IReadOnlyBrowserSession : IAsyncDisposable
{
    Task NavigateAsync(string url, CancellationToken cancellationToken);
    Task<(string Url, string Title, string TextContent)> ReadPageAsync(CancellationToken cancellationToken);
    Task<Dictionary<string, string?>> ExtractStructuredDataAsync(IReadOnlyDictionary<string, string> selectors, CancellationToken cancellationToken);
    Task<bool> WaitForElementAsync(string selector, TimeSpan timeout, CancellationToken cancellationToken);
    Task<string> TakeScreenshotBase64Async(CancellationToken cancellationToken);
}

public sealed class BrowserLifetime : IReadOnlyBrowserSession
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly IBrowserContext _context;
    private readonly IPage _page;
    private readonly HashSet<string> _allowedDomains;
    private volatile SecurityException? _policyViolation;

    public BrowserLifetime(
        IPlaywright playwright,
        IBrowser browser,
        IBrowserContext context,
        IPage page,
        IEnumerable<string> allowedDomains)
    {
        _playwright = playwright;
        _browser = browser;
        _context = context;
        _page = page;
        _allowedDomains = new HashSet<string>(allowedDomains, StringComparer.OrdinalIgnoreCase);

        _page.Dialog += async (_, dialog) => await dialog.DismissAsync();
        _page.Download += async (_, download) => await download.CancelAsync();
        _page.FileChooser += (_, _) => throw new SecurityException("File uploads are disabled in read-only mode.");
        _page.FrameNavigated += (_, frame) =>
        {
            if (frame.ParentFrame is not null)
            {
                return;
            }

            if (Uri.TryCreate(frame.Url, UriKind.Absolute, out var uri) && !_allowedDomains.Contains(uri.Host))
            {
                _policyViolation = new SecurityException($"Navigation to disallowed domain '{uri.Host}' was blocked.");
            }
        };
    }

    public async Task NavigateAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !_allowedDomains.Contains(uri.Host))
        {
            throw new SecurityException("Navigation blocked by domain allowlist.");
        }

        await _page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        EnsurePolicyNotViolated();
        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task<(string Url, string Title, string TextContent)> ReadPageAsync(CancellationToken cancellationToken)
    {
        EnsurePolicyNotViolated();
        var title = await _page.TitleAsync();
        var text = await _page.TextContentAsync("body") ?? string.Empty;
        cancellationToken.ThrowIfCancellationRequested();
        return (_page.Url, title, text.Trim());
    }

    public async Task<Dictionary<string, string?>> ExtractStructuredDataAsync(IReadOnlyDictionary<string, string> selectors, CancellationToken cancellationToken)
    {
        EnsurePolicyNotViolated();
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var pair in selectors)
        {
            var value = await _page.TextContentAsync(pair.Value);
            result[pair.Key] = value?.Trim();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    public async Task<bool> WaitForElementAsync(string selector, TimeSpan timeout, CancellationToken cancellationToken)
    {
        EnsurePolicyNotViolated();
        try
        {
            await _page.WaitForSelectorAsync(selector, new() { Timeout = (float)timeout.TotalMilliseconds, State = WaitForSelectorState.Visible });
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public async Task<string> TakeScreenshotBase64Async(CancellationToken cancellationToken)
    {
        EnsurePolicyNotViolated();
        var bytes = await _page.ScreenshotAsync(new()
        {
            FullPage = true,
            Type = ScreenshotType.Png,
            Animations = ScreenshotAnimations.Disabled
        });

        cancellationToken.ThrowIfCancellationRequested();
        return Convert.ToBase64String(bytes);
    }

    public async ValueTask DisposeAsync()
    {
        await _page.CloseAsync(new() { RunBeforeUnload = false });
        await _context.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    private void EnsurePolicyNotViolated()
    {
        if (_policyViolation is not null)
        {
            throw _policyViolation;
        }
    }
}
