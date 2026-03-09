using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using YukariApp.GameTracker;

namespace YukariLauncher.Config;

public partial class YukariConfig : Node
{
    public static YukariConfig Instance { get; set; }
    private const string AppConfigFileName = "AppConfig.json";
    public AppConfigData ConfigData { get; set; } = new();

    [JsonSerializable(typeof(AppConfigData))]
    internal partial class AppConfigContext : JsonSerializerContext { }

    private static string AppConfigFilePath =>
        $"" +
        $"{Yukari.UserPath}" +
        $"/{AppConfigFileName}";

    public event Action OnBeforeConfigSave;

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
        LoadInto(this);

        if (ConfigData.DownloadPath.IsNullOrEmpty())
        {
            ConfigData.DownloadPath = Yukari.DefaultDownloadPath;
        }

        if (ConfigData.InstallPath.IsNullOrEmpty())
        {
            ConfigData.InstallPath = Yukari.DefaultInstallPath;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        if (Yukari.Instance is null)
        {
            GD.PrintErr("Errrrrrror");
            return;
        }

        Yukari.Instance.AppClosed += Save;
    }

    private void AppOnGameStartedEvent(GameEntryResource gameEntry)
    {
        if (ConfigData.GameRecords.TryGetValue(gameEntry.Id, out var record))
        {
            record.LastPlayed = DateTime.Now;
        }
        else
        {
            AddGameRecord(gameEntry, null);
        }
    }

    public void Save()
    {
        OnBeforeConfigSave?.Invoke();

        if (!File.Exists(AppConfigFilePath))
        {
            File.Create(AppConfigFilePath).Close();
        }

        var jsonString = JsonSerializer.Serialize(ConfigData, AppConfigContext.Default.AppConfigData);
        File.WriteAllText(AppConfigFilePath, jsonString);
    }

    public void LoadInto(YukariConfig target)
    {
        if (File.Exists(AppConfigFilePath))
        {
            var loaded = JsonSerializer.Deserialize(File.ReadAllText(AppConfigFilePath),
                AppConfigContext.Default.AppConfigData);
            target.ConfigData.GameRecords = loaded?.GameRecords ?? [];
        }
    }

    public void AddGameRecord(GameEntryResource gameEntry, string installPath)
    {
        var gameRecord = new GameRecord
        {
            InstallLocation = installPath,
            LastPlayed      = DateTime.MinValue,
            TimePlayed      = 0,
        };
        ConfigData.GameRecords.TryAdd(gameEntry.Id, gameRecord);
    }

    public static string GetApiAddress()
    {
        return Instance.ConfigData.ApiAddress;
    }

    public static DateTime GetGameLastPlayed(string id)
    {
        return Instance.ConfigData.GameRecords.TryGetValue(id, out var record)
            ? record?.LastPlayed ?? DateTime.MinValue
            : DateTime.MinValue;
    }

    public static long GetGamePlayTime(string id)
    {
        return Instance.ConfigData.GameRecords.TryGetValue(id, out var record) ? record.TimePlayed : 0;
    }

    public static string GetGameInstallPath(string id)
    {
        return Instance.ConfigData.GameRecords.TryGetValue(id, out var record) ? record.InstallLocation : null;
    }

    public static ThEnums.Difficulty GetGameHighestDifficulty(string id)
    {
        return Instance.ConfigData.GameRecords.TryGetValue(id, out var record)
            ? record.HighestDifficultyBeaten
            : ThEnums.Difficulty.None;
    }

    public static bool GetGameExtraStageBeaten(string id)
    {
        return Instance.ConfigData.GameRecords.TryGetValue(id, out var record) && record.ExtraStageBeaten;
    }
}

public class AppConfigData
{
    public string ApiAddress { get; set; } = Ran.DefaultApiAddress;
    public string InstallPath { get; set; }
    public string DownloadPath { get; set; }
    public bool LocalMode { get; set; } = true;
    public Dictionary<string, GameRecord> GameRecords { get; set; } = [];
}