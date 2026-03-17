using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using Godot;
using NexusMods.Paths;
using YukariApp.Common.JsonConverters;
using YukariApp.GameTracker;

namespace YukariLauncher.Config;

public partial class YukariConfig : Node
{
    public static YukariConfig Instance { get; set; }
    private const string AppConfigFileName = "AppConfig.json";
    public AppConfigData ConfigData { get; set; } = new();

    [JsonSerializable(typeof(AppConfigData)),
     JsonSourceGenerationOptions(Converters = [typeof(Vector2IJsonConverter)], WriteIndented = true)]
    internal partial class AppConfigContext : JsonSerializerContext { }

    private static string AppConfigFilePath =>
        $"" +
        $"{Yukari.UserPath}" +
        $"/{AppConfigFileName}";

    public event Action OnBeforeConfigSave;

    private static readonly SteamHandler SteamGameLocator =
        new(FileSystem.Shared, OperatingSystem.IsWindows() ? WindowsRegistry.Shared : null);

    private bool _configLoaded = false;

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
        LoadInto(this);
        // GetWindow().SetSize(ConfigData.WindowSize);
        // GetWindow().MoveToCenter();

        if (ConfigData.DownloadPath.IsNullOrEmpty())
        {
            ConfigData.DownloadPath = Yukari.DefaultDownloadPath;
        }

        if (ConfigData.InstallPath.IsNullOrEmpty())
        {
            ConfigData.InstallPath = Yukari.DefaultInstallPath;
        }

        if (ConfigData.DosboxPath.IsNullOrEmpty())
        {
            ConfigData.DosboxPath = ConfigData.InstallPath + "/pc98";
        }
    }

    public override void _Ready()
    {
        base._Ready();

        Yukari.Instance?.AppClosed += Save;
        Chen.Instance?.GameClosed  += _ => Save();
    }

    private void AppOnGameStartedEvent(GameEntryResource gameEntry)
    {
        if (ConfigData.GameRecords.TryGetValue(gameEntry.Id, out var record))
        {
            record.LastPlayed = DateTime.Now;
        }
        else
        {
            AddOrUpdateGameRecord(gameEntry, null);
        }
    }

    public void Save()
    {
        if (!_configLoaded)
        {
            return;
        }

        if (GetWindow().Mode != Window.ModeEnum.Maximized)
        {
            ConfigData.ScreenSize = GetWindow().GetSize();
        }

        ConfigData.Maximized = GetWindow().GetMode() == Window.ModeEnum.Maximized;
        OnBeforeConfigSave?.Invoke();
        if (!File.Exists(AppConfigFilePath))
        {
            File.Create(AppConfigFilePath).Close();
        }

        if (ConfigData.ApiAddress.IsNullOrEmpty())
        {
            ConfigData.ApiAddress = ProjectSettings.GetSetting("application/config/default_ran_api").AsStringName();
        }

        try
        {
            var json = JsonSerializer.Serialize(Instance.ConfigData,
                AppConfigContext.Default.AppConfigData);

            GD.Print("Config Saved");
            File.WriteAllText(AppConfigFilePath, json);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to save config: {e.Message}");
        }
    }

    public void LoadInto(YukariConfig target)
    {
        if (!File.Exists(AppConfigFilePath))
        {
            target.ConfigData = new AppConfigData();
            _configLoaded     = true;
            return;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize(File.ReadAllText(AppConfigFilePath),
                AppConfigContext.Default.AppConfigData);

            if (loaded is null)
            {
                return;
            }

            target.ConfigData = loaded;
            _configLoaded     = true;
        }
        catch (JsonException e)
        {
            GD.PrintErr($"Failed to load, wouldn't want to wipe data now would we: {e.Message}");
        }
    }

    public void AddOrUpdateGameRecord(GameEntryResource gameEntry, string installPath)
    {
        var hasRecord = ConfigData.GameRecords.TryGetValue(gameEntry.Id, out var gameRecord);
        if (!hasRecord)
        {
            gameRecord = new GameRecord
            {
                InstallLocation = installPath,
                LastPlayed      = DateTime.MinValue,
                TimePlayed      = 0,
            };
            ConfigData.GameRecords.TryAdd(gameEntry.Id, gameRecord);
        }
        else
        {
            gameRecord.InstallLocation = installPath;
        }
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

    public static bool IsGameDownloadable(string id)
    {
        return Instance.ConfigData.DownloadableGamesCache.Contains(id);
    }
}

public class AppConfigData
{
    public string ApiAddress { get; set; } =
        ProjectSettings.GetSetting("application/config/default_ran_api").AsStringName();

    public List<string> DownloadableGamesCache { get; set; } = [];
    public Vector2I ScreenSize { get; set; } = new(1152, 648);
    public bool Maximized { get; set; } = false;
    public bool DiscordRpcEnabled { get; set; }
    public float UiScale { get; set; } = 1f;
    public int CardColumns { get; set; } = 4;
    public string InstallPath { get; set; }
    public string DownloadPath { get; set; }
    public string DosboxPath { get; set; }
    public bool LocalMode { get; set; } = true;
    public Dictionary<string, GameRecord> GameRecords { get; set; } = [];
}