using System;
using System.Text.RegularExpressions;
using Godot;

namespace YukariLauncher;

[GlobalClass, Tool]
public partial class GameEntryResource : Resource
{
    [Export(PropertyHint.MultilineText)] public string Name;
    [Export] public GameType Type = GameType.Mainline;
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
}

[Flags]
public enum GameType
{
    Mainline = 1 << 0,
    PC98 = 1 << 1,
    Fighter = 1 << 2,
    Spinoff = 1 << 3,
}