using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SptTraderConfig = SPTarkov.Server.Core.Models.Spt.Config.TraderConfig;

namespace TraderLevelRequirementsEditor;

[Injectable(InjectionType.Transient, int.MaxValue)]
public sealed class TraderLevelRequirementsEditor : IOnUpdate
{
    private const string LogPrefix = "[Trader Level Requirements Editor] ";

    private readonly ISptLogger<TraderLevelRequirementsEditor> _logger;

    public TraderLevelRequirementsEditor(
        TradersTable tradersTable,
        SptTraderConfig serverTraderConfig,
        ModHelper modHelper,
        ISptLogger<TraderLevelRequirementsEditor> logger)
    {
        _logger = logger;

        string modFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        string configPath = System.IO.Path.Combine(modFolder, "config", "traders.jsonc");
        TraderEditorConfig config = JsonSerializer.Deserialize<TraderEditorConfig>(
            File.ReadAllText(configPath),
            new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            }) ?? throw new InvalidOperationException($"Configuration file contains no JSON data: {configPath}");

        ApplyPatches(tradersTable, serverTraderConfig, config);
    }

    public Task<bool> OnUpdateAsync(long secondsSinceLastRun, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    private void ApplyPatches(TradersTable tradersTable, SptTraderConfig serverTraderConfig, TraderEditorConfig config)
    {
        foreach ((string traderId, TraderPatch patch) in config.Traders)
        {
            MongoId traderMongoId = new(traderId);
            Trader? trader = tradersTable.GetTrader(traderMongoId);
            if (trader is null)
            {
                _logger.Warning($"{LogPrefix}Trader not found: {traderId}");
                continue;
            }

            ApplyLoyaltyLevelPatches(trader, patch);
            ApplyRefreshTimePatch(serverTraderConfig, traderMongoId, patch);
            _logger.Info($"{LogPrefix}Updated {patch.Name ?? traderId}");
        }
    }

    private void ApplyRefreshTimePatch(SptTraderConfig serverTraderConfig, MongoId traderId, TraderPatch patch)
    {
        if (patch.RefreshSeconds is null)
        {
            return;
        }

        string traderName = patch.Name ?? traderId.ToString();
        if (traderId == Traders.FENCE)
        {
            _logger.Warning($"{LogPrefix}Refresh-time settings are not supported for Fence; ignoring settings for {traderName}");
            return;
        }

        SPTarkov.Server.Core.Models.Spt.Config.UpdateTime? updateTime =
            serverTraderConfig.UpdateTime.FirstOrDefault(entry => entry.TraderId == traderId);
        if (updateTime is null)
        {
            _logger.Warning($"{LogPrefix}No refresh-time entry found for {traderName}");
            return;
        }

        int min = patch.RefreshSeconds.Min ?? updateTime.Seconds.Min;
        int max = patch.RefreshSeconds.Max ?? updateTime.Seconds.Max;
        if (min < 0 || max < 0 || min > max)
        {
            _logger.Warning($"{LogPrefix}Invalid refresh-time range for {traderName}: min={min}, max={max}");
            return;
        }

        updateTime.Seconds.Min = min;
        updateTime.Seconds.Max = max;
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

public sealed class TraderEditorConfig
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

    [JsonPropertyName("refreshSeconds")]
    public RefreshSecondsPatch? RefreshSeconds { get; set; }
}

public sealed class RefreshSecondsPatch
{
    [JsonPropertyName("min")]
    public int? Min { get; set; }

    [JsonPropertyName("max")]
    public int? Max { get; set; }
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