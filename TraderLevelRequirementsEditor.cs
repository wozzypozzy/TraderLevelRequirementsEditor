using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace TraderLevelRequirementsEditor;

[Injectable(InjectionType.Transient, int.MaxValue)]
public sealed class TraderLevelRequirementsEditor : IOnUpdate
{
    private const string LogPrefix = "[Trader Level Requirements Editor] ";

    private readonly ISptLogger<TraderLevelRequirementsEditor> _logger;

    public TraderLevelRequirementsEditor(
        TradersTable tradersTable,
        ModHelper modHelper,
        ISptLogger<TraderLevelRequirementsEditor> logger)
    {
        _logger = logger;

        string modFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        TraderConfig config = modHelper.GetJsonDataFromFile<TraderConfig>(modFolder, "config/traders.json");

        ApplyPatches(tradersTable, config);
    }

    public Task<bool> OnUpdateAsync(long secondsSinceLastRun, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    private void ApplyPatches(TradersTable tradersTable, TraderConfig config)
    {
        foreach ((string traderId, TraderPatch patch) in config.Traders)
        {
            Trader? trader = tradersTable.GetTrader(new MongoId(traderId));
            if (trader is null)
            {
                _logger.Warning($"{LogPrefix}Trader not found: {traderId}");
                continue;
            }

            ApplyLoyaltyLevelPatches(trader, patch);
            _logger.Info($"{LogPrefix}Updated {patch.Name ?? traderId}");
        }
    }

    private void ApplyLoyaltyLevelPatches(Trader trader, TraderPatch patch)
    {
        if (patch.LoyaltyLevels is null || trader.Base.LoyaltyLevels is null)
        {
            return;
        }

        foreach ((string levelNumber, LoyaltyLevelPatch levelPatch) in patch.LoyaltyLevels)
        {
            if (!int.TryParse(levelNumber, out int level))
            {
                _logger.Warning($"{LogPrefix}Invalid loyalty level '{levelNumber}' for {patch.Name ?? trader.Base.Id.ToString()}");
                continue;
            }

            int levelIndex = level - 1;
            if (levelIndex < 0 || levelIndex >= trader.Base.LoyaltyLevels.Count)
            {
                _logger.Warning($"{LogPrefix}{patch.Name ?? trader.Base.Id.ToString()} has no loyalty level {levelNumber}");
                continue;
            }

            TraderLoyaltyLevel loyaltyLevel = trader.Base.LoyaltyLevels[levelIndex];
            loyaltyLevel.MinLevel = levelPatch.MinLevel ?? loyaltyLevel.MinLevel;
            loyaltyLevel.MinSalesSum = levelPatch.MinSalesSum ?? loyaltyLevel.MinSalesSum;
            loyaltyLevel.MinStanding = levelPatch.MinStanding ?? loyaltyLevel.MinStanding;
        }
    }
}

public sealed class TraderConfig
{
    [JsonPropertyName("traders")]
    public Dictionary<string, TraderPatch> Traders { get; set; } = new();
}

public sealed class TraderPatch
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("loyaltyLevels")]
    public Dictionary<string, LoyaltyLevelPatch>? LoyaltyLevels { get; set; }
}

public sealed class LoyaltyLevelPatch
{
    [JsonPropertyName("minLevel")]
    public int? MinLevel { get; set; }

    [JsonPropertyName("minSalesSum")]
    public long? MinSalesSum { get; set; }

    [JsonPropertyName("minStanding")]
    public double? MinStanding { get; set; }
}