using System.Collections.Generic;
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace TraderLevelRequirementsEditor;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "wozzy.spt.traderlevelrequirementseditor";

    public string Name { get; init; } = "Trader Level Requirements Editor";

    public string Author { get; init; } = "wozzy";

    public List<string>? Contributors { get; init; }

    public Version Version { get; init; } = new Version("1.0.0");

    public Range SptVersion { get; init; } = new Range("~4.1.0");

    public List<string>? Incompatibilities { get; init; }

    public Dictionary<string, Range>? ModDependencies { get; init; }

    public string? Url { get; init; }

    public bool? IsBundleMod { get; init; }

    public string License { get; init; } = "MIT";

    public bool HasPrepatcher { get; init; }
}