using System;
using System.Collections.Generic;

namespace META7.Operator.Api.Outreach;

public sealed record ReadOnlyPageSnapshot(
    Uri TargetUri,
    string BodyText,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyList<string> StructuralCues);
