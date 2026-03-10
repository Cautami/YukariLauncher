using System;
using System.Text.RegularExpressions;
using Godot;

namespace YukariLauncher;

[GlobalClass, Tool]
public partial class GameEntryResource : Resource
{
    [Export(PropertyHint.MultilineText)] public string Name;
    [Export] public GameChronology Chronology = GameChronology.Mainline;
    [Export] public GameType Type = GameType.Standard;
    [Export] public string Id { get; set; }
    [Export] public uint SteamAppId { get; set; }
    [Export, ExportGroup("File Names")] public string ExeName;
    [Export] public string ProcessName;
    [Export] public string PatcherExeName;
    [Export] public string PatcherConfigExeName;
    [Export] public string ConfigFileName;
    [Export, ExportGroup("Art")] public Texture2D CoverArt;
    [Export] public Texture2D BannerArt;
    [Export] public Texture2D LogoArt;
    [Export] public Texture2D IconArt;
    [Export, ExportGroup("")] public Vector3I ReleaseDate = new(0, 8, 0);
    [Export] public string Author = "Team Shanghai Alice (ZUN)";
    [Export, ExportGroup("Debug")] public bool InTesting;
    [Export] public bool IsNativeConfig;

    public string GetGameTypeName()
    {
        switch (Type)
        {
            case GameType.Standard:
                return "Vertical Shooting Game";
                break;
            case GameType.Fighter:
                return "Fighting Game";
                break;
            case GameType.Photo:
                return "Vertical Photography Shooting Game";
                break;
            case GameType.Freezing:
                return "Vertical Freezing Game";
                break;
            case GameType.Puzzle:
                return "Puzzle Shooting Game";
                break;
            case GameType.Versus:
                return "Competitive Vertical Shooting Game";
                break;
            case GameType.Horizontal:
                return "Horizontal Water Action Game";
                break;
            case GameType.Scrollless:
                return "Scroll-less Action Shooting Game";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public enum GameType
{
    Standard = 1 << 0,
    Fighter = 1 << 2,
    Photo = 1 << 4,
    Freezing = 1 << 5,
    Puzzle = 1 << 6,
    Versus = 1 << 7,
    Horizontal = 1 << 8,
    Scrollless = 1 << 9,
}

public enum GameChronology
{
    PC98 = 1 << 1,
    Mainline,
    Spinoff,
}