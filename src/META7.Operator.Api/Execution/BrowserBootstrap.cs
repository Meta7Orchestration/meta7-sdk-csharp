using Microsoft.Playwright;

namespace META7.Operator.Api.Execution;

public interface IBrowserBootstrap
{
    Task<IReadOnlyBrowserSession> CreateSessionAsync(OperatorExecutionOptions options, CancellationToken cancellationToken);
}

public sealed class BrowserBootstrap : IBrowserBootstrap
{
    public async Task<IReadOnlyBrowserSession> CreateSessionAsync(OperatorExecutionOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args =
            [
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage"
            ]
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = options.ViewportWidth, Height = options.ViewportHeight },
            UserAgent = options.UserAgent,
            Locale = "en-US",
            TimezoneId = "UTC",
            AcceptDownloads = false,
            IgnoreHTTPSErrors = false
        });

        await context.RouteAsync("**/*", async route =>
        {
            var method = route.Request.Method;
            if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                await route.AbortAsync();
                return;
            }

            await route.ContinueAsync();
        });

        await context.AddInitScriptAsync(@"
            (() => {
                const blocked = () => { throw new Error('Read-only mode blocks DOM mutation'); };
                document.write = blocked;
                document.writeln = blocked;
                Element.prototype.append = blocked;
                Element.prototype.prepend = blocked;
                Element.prototype.remove = blocked;
                Element.prototype.replaceWith = blocked;
                Element.prototype.insertAdjacentHTML = blocked;
                Element.prototype.insertAdjacentElement = blocked;
                Element.prototype.setAttribute = blocked;
                Element.prototype.removeAttribute = blocked;
                Element.prototype.toggleAttribute = blocked;
                Element.prototype.replaceChildren = blocked;
                Node.prototype.appendChild = blocked;
                Node.prototype.removeChild = blocked;
                Node.prototype.replaceChild = blocked;
                Node.prototype.insertBefore = blocked;
                Range.prototype.createContextualFragment = blocked;
                Object.defineProperty(Element.prototype, 'innerHTML', { set: blocked });
                Object.defineProperty(Element.prototype, 'outerHTML', { set: blocked });
                Object.defineProperty(Node.prototype, 'textContent', { set: blocked });
            })();
        ");

        var page = await context.NewPageAsync();
        return new BrowserLifetime(playwright, browser, context, page, options.AllowedDomains);
    }
}
