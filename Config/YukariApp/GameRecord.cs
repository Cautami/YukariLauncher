using System;
using YukariApp.GameTracker;

namespace YukariLauncher.Config;

public record GameRecord
{
    public string InstallLocation { get; set; }
    public DateTime LastPlayed { get; set; }
    public long TimePlayed { get; set; }
    public ThEnums.Difficulty HighestDifficultyBeaten { get; set; }
    public bool ExtraStageBeaten { get; set; }
}