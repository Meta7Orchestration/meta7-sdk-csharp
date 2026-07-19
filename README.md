# META7 SDK C#

## META7 Synthetic Operator (Embodied Operator)

This repository now includes a read-only browser execution layer for the Operator API.

### Architecture

- `PlaywrightOperatorExecutor`: executes read-only browser directives
- `BrowserBootstrap`: initializes deterministic headless Chromium sessions
- `BrowserLifetime`: manages browser/page lifecycle and deterministic cleanup
- `DirectiveExecutionService`: routes read-only browser actions and enforces SAFE_LOCK gating

### Supported read-only actions

- `Navigate`
- `ReadPage`
- `ExtractStructuredData`
- `WaitForElement`
- `TakeScreenshot`

### Deterministic headless mode

- Headless Chromium only
- Fixed viewport (`1280x720`)
- Fixed user agent (`META7-SyntheticOperator/1.0 (Deterministic)`)
- Locale and timezone pinned (`en-US`, `UTC`)
- Per-directive timeout enforcement

### Safety and policy guarantees

- SAFE_LOCK rejection enforced before execution
- Domain allowlist enforcement for all navigations
- JavaScript dialogs dismissed
- Downloads disabled
- Uploads blocked
- Non-GET/HEAD requests aborted
- Read-only mode blocks DOM mutation APIs

### Local execution

From repository root:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Install Playwright browser binaries for local real-browser execution:

```bash
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```
