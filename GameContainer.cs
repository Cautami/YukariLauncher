using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using YukariLauncher;

public partial class GameContainer : GridContainer
{
    [Export] private PackedScene _gameEntryScene;

    public event Action<GameEntryResource> CardHovered;

    public List<GameEntry> _gameEntries = [];

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
            _gameEntries.Add(gameEntry);
        }
    }

    private void OnControlOnSearchBarUpdated(string searchBarText)
    {
        var search = searchBarText.ToLower();
        GameType? typeFilter = null;
        GameChronology? chronologyFilter = null;
        var cleanSearch = search;

        var parts = cleanSearch.Split(' ').ToList();
        var tagsToRemove = new List<string>();

        foreach (var part in parts)
        {
            if (!part.StartsWith('@'))
            {
                continue;
            }

            var tag = part[1..];

            if (typeFilter == null && Enum.TryParse<GameType>(tag, true, out var parsedType))
            {
                typeFilter = parsedType;
                tagsToRemove.Add(part);
            }
            else if (chronologyFilter == null && Enum.TryParse<GameChronology>(tag, true, out var parsedChronology))
            {
                chronologyFilter = parsedChronology;
                tagsToRemove.Add(part);
            }
        }

        cleanSearch = tagsToRemove.Aggregate(cleanSearch, (current, tag) => current.Replace(tag, "").Trim());

        cleanSearch = string.Join(' ', cleanSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        foreach (var gameEntry in _gameEntries)
        {
            var name = gameEntry.EntryResource.Name.ToLower();
            var id = gameEntry.EntryResource.Id.ToLower();
            var matchesText = string.IsNullOrEmpty(cleanSearch) || name.Contains(cleanSearch) ||
                              id.Contains(cleanSearch);
            var matchesType = typeFilter == null || gameEntry.EntryResource.Type == typeFilter;
            var matchesChronology =
                chronologyFilter == null || gameEntry.EntryResource.Chronology == chronologyFilter;
            gameEntry.Visible = matchesText && matchesType && matchesChronology;
        }
    }
}