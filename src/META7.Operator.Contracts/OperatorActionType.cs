namespace META7.Operator.Contracts;

/// <summary>
/// Describes the type of browser/page action that an Operator directive requests.
/// </summary>
public enum OperatorActionType
{
    // ── Safe read-only actions accepted by ReadOnlyOperatorExecutor ──────────
    HealthCheck,
    Navigate,
    ReadPage,
    ExtractStructuredData,
    TakeScreenshot,
    WaitForElement,

    // ── Unsafe / mutating actions — always rejected by ReadOnlyOperatorExecutor
    ExecuteScript,
    SubmitForm,
    ClickElement,
    FillInput,
    Download,
    Upload,
    DeleteResource,
    ModifyDom
}
