using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using GameFinder.StoreHandlers.Steam.Services;
using Godot;
using IniParser;
using IniParser.Model;
using NexusMods.Paths;
using YukariLauncher;
using YukariLauncher.Config;

public partial class Chen : Node
{
    public static Chen Instance { get; private set; }

    private static readonly SteamHandler SteamGameLocator =
        new(FileSystem.Shared, OperatingSystem.IsWindows() ? WindowsRegistry.Shared : null);

    private readonly Dictionary<string, Process> _runningGames = new();
    private readonly Dictionary<string, double> _gameTimers = new();
    private double _saveTimer = 0;
    private const int SaveInterval = 1;

    public event Action<GameEntryResource> GameStarted;
    public event Action<GameEntryResource> GameDetected;

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
        if (!(_saveTimer >= SaveInterval))
        {
            return;
        }

        _saveTimer = 0;
        SaveGameTimers();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
    }

    public override void _Ready()
    {
        base._Ready();

        GameDetected += _ =>
        {
            SaveGameDataPlayed();
        };
    }

    public async Task StartGame(GameEntryResource gameEntry, bool isSteam)
    {
        if (gameEntry is null)
        {
            GD.PrintErr("Tried to start a game, but the Entry resource was null");
            return;
        }

        var path = YukariConfig.GetGameInstallPath(gameEntry.Id);
        var exe = gameEntry.PatcherExeName;
        if (exe.IsNullOrEmpty())
        {
            exe = gameEntry.ExeName;
        }

        Callable.From(() => GameStarted?.Invoke(gameEntry)).CallDeferred();

        if (isSteam)
        {
            OS.ShellOpen($"steam://launch/{gameEntry.SteamAppId}");
        }
        else if (gameEntry.Chronology == GameChronology.PC98)
        {
            OpenPc98(gameEntry.Id);
        }
        else
        {
            OpenProcess(path + "/" + exe);
        }

        var processName = gameEntry.Chronology == GameChronology.PC98 ? "dosbox-x.exe" : gameEntry.ProcessName;
        var gameProcess = await FindProcess(processName);
        SaveGameDataPlayed();
        Callable.From(() => GameDetected?.Invoke(gameEntry)).CallDeferred();
        _runningGames.Add(gameEntry.Id, gameProcess);
        gameProcess.EnableRaisingEvents = true;
        gameProcess.Exited += (_, _) =>
        {
            CallDeferred(nameof(SaveGameTimers));
            Callable.From(() => GameClosed?.Invoke(gameEntry)).CallDeferred();
        };
    }

    private void OpenPc98(string id)
    {
        UpdatePc98Conf(id);
        OpenProcess(YukariConfig.Instance.ConfigData.DosboxPath + "/dosbox-x.exe");
    }

    private void UpdatePc98Conf(string id)
    {
        var path = YukariConfig.Instance.ConfigData.DosboxPath + "/dosbox-x.conf";
        var lines = File.ReadAllLines(path);

        var autoexecIndex = Array.FindIndex(lines, l => l.Trim() == "[autoexec]");
        if (autoexecIndex == -1)
        {
            return;
        }

        var updated = lines[..autoexecIndex].Concat(new[]
        {
            "[autoexec]",
            @"mount c: .\touhou",
            "c:",
            $@"imgmount d: ""{id}e.hdi""",
            "d:",
            "game",
        });

        File.WriteAllLines(path, updated);
    }

    public void StopGame(string id)
    {
        if (!_runningGames.TryGetValue(id, out var value))
        {
            return;
        }

        value.Kill();
        value.WaitForExit();
        value.Dispose();
        _runningGames.Remove(id);
    }

    private static void OpenProcess(string exePath)
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

    public static bool IsGameInstalled(GameEntryResource gameEntry, out bool isSteam)
    {
        var installPath = YukariConfig.GetGameInstallPath(gameEntry.Id);
        // ReSharper disable once InvertIf
        //I think it looks better like this
        if (installPath.IsNullOrEmpty() || !File.Exists($"{installPath}/{gameEntry.ExeName}"))
        {
            if (gameEntry.SteamAppId == 0)
            {
                isSteam = false;
                return false;
            }

            var gameInfo = SteamGameLocator.FindOneGameById(AppId.From(gameEntry.SteamAppId), out var errors);

            foreach (var error in errors)
            {
                GD.PrintErr(error);
            }

            isSteam = true;
            return gameInfo is not null;
        }

        isSteam = false;
        return File.Exists($"{installPath}/{gameEntry.ExeName}");
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
            if (!YukariConfig.Instance.ConfigData.GameRecords.TryGetValue(id, out var record))
            {
                continue;
            }

            record.LastPlayed = DateTime.UtcNow;
        }
    }
}