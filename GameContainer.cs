using Godot;
using System;
using System.Linq;
using YukariLauncher;

public partial class GameContainer : GridContainer
{
    [Export] private PackedScene _gameEntryScene;

    public event Action<GameEntryResource> CardHovered;

    //90% Sure this should just be merged into Yukari.cs
    public override void _Ready()
    {
        if (_gameEntryScene is null)
        {
            return;
        }

        foreach (var gameEntryResource in ResourceHelper.GetAll<GameEntryResource>().ToList().OrderBy(x => x.Id))
        {
            var gameEntry = _gameEntryScene.Instantiate<GameEntry>();
            gameEntry.EntryResource = gameEntryResource;
            AddChild(gameEntry);
            gameEntry.MouseEntered += () => CardHovered?.Invoke(gameEntry.EntryResource);
        }
    }
}