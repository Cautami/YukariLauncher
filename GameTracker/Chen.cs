using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using GameFinder.StoreHandlers.Steam.Services;
using Godot;
using NexusMods.Paths;
using YukariLauncher;
using YukariLauncher.Config;

public partial class Chen : Node
{
    public static Chen Instance { get; private set; }

    public static SteamHandler SteamGameLocator =
        new(FileSystem.Shared, OperatingSystem.IsWindows() ? WindowsRegistry.Shared : null);

    private Dictionary<string, Process> _runningGames = new();
    private Dictionary<string, double> _gameTimers = new();
    private double _saveTimer = 0;
    private const int _saveInterval = 5;

    public event Action<GameEntryResource> GameStarted;
    public event Action<GameEntryResource> GameClosed;
    public event Action<GameEntryResource> GameUpdated;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_runningGames.Count <= 0)
        {
            return;
        }

        foreach (var game in _runningGames)
        {
            _gameTimers.TryAdd(game.Key, 0);
            _gameTimers[game.Key] += delta;
        }

        _saveTimer += delta;
        if (_saveTimer >= _saveInterval)
        {
            _saveTimer = 0;
            SaveGameTimers();
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
    }

    public override void _Ready()
    {
        base._Ready();

        GameStarted += _ =>
        {
            CallDeferred(nameof(SaveGameDataPlayed));
        };
    }

    public async Task StartGame(GameEntryResource gameEntry)
    {
        if (gameEntry is null)
        {
            GD.PrintErr("Tried to start a game, but the Entry resource was null");
            return;
        }

        var path = YukariConfig.GetGameInstallPath(gameEntry.Id);
        OpenProcess(path + "/" + gameEntry.PatcherExeName);

        var gameProcess = await FindProcess(gameEntry.ProcessName);
        SaveGameDataPlayed();
        GameStarted?.Invoke(gameEntry);
        _runningGames.Add(gameEntry.Id, gameProcess);
        gameProcess.EnableRaisingEvents = true;
        gameProcess.Exited += (_, _) =>
        {
            CallDeferred(nameof(SaveGameTimers));
            GameClosed?.Invoke(gameEntry);
        };
    }

    private void OpenProcess(string exePath)
    {
        ProcessStartInfo startInfo;
        if (OS.GetName() == "Linux")
        {
            startInfo = new ProcessStartInfo
            {
                FileName         = "wine",
                Arguments        = $"\"{exePath}\"",
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty,
            };
        }
        else
        {
            startInfo = new ProcessStartInfo
            {
                FileName         = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty,
            };
        }

        if (startInfo.FileName.IsNullOrEmpty())
        {
            return;
        }

        Process.Start(startInfo);
    }

    public static bool IsGameInstalled(GameEntryResource gameEntry)
    {
        var yukariPath = YukariConfig.GetGameInstallPath(gameEntry.Id);
        if (yukariPath.IsNullOrEmpty())
        {
            if (gameEntry.SteamAppId == 0)
            {
                return false;
            }

            var gameInfo = SteamGameLocator.FindOneGameById(AppId.From(gameEntry.SteamAppId), out var errors);

            foreach (var error in errors)
            {
                GD.PrintErr(error);
            }

            return gameInfo is not null;
        }

        return File.Exists($"{yukariPath}/{gameEntry.ProcessName}");
    }

    //stupid linux doesnt pass me a full path back
    //ergo i cant actually determine whether the process truly belongs to me
    // :(
    //i can only hope there isnt software conveniently named the same as a touhou game
    //Regardless, the limit is 30 seconds due to modern games typically showing a config screen
    //If I could find a way to bypass that...
    private const int ProcessTimeout = 30000;

    private async Task<Process> FindProcess(string processName)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < ProcessTimeout)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                GD.Print("Found game");
                return processes[0];
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Could not find {processName} within a reasonable amount of time.");
    }

    private void SaveGameTimers()
    {
        foreach (var (id, seconds) in _gameTimers)
        {
            if (YukariConfig.Instance.ConfigData.GameRecords.TryGetValue(id, out var record))
            {
                record.TimePlayed += (long)seconds;
            }
        }

        _gameTimers.Clear();
    }

    private void SaveGameDataPlayed()
    {
        foreach (var (id, _) in _runningGames)
        {
            if (YukariConfig.Instance.ConfigData.GameRecords.TryGetValue(id, out var record))
            {
                GD.Print($"Saved date for {id}");
                record.LastPlayed = DateTime.UtcNow;
            }
        }
    }
}