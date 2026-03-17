using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;
using GTweens.Builders;
using GTweens.Easings;
using GTweens.Tweens;
using GTweensGodot.Extensions;

public partial class FindGameBar : Control
{
    [Export] private LineEdit _searchBar;

    [Export] private RichTextLabel _richSearchBar;
    [Export] private float _tweenDuration;

    [Signal]
    public delegate string OnSearchBarUpdatedEventHandler(string newText);

    private readonly Shortcut _searchShortcut = new();

    private bool _isSearching = false;

    private GTween _currentTween;

    public override void _Ready()
    {
        _searchBar.TextChanged += SearchBarOnTextChanged;

        var shortcutEvent = new InputEventKey
        {
            Keycode                   = Key.F,
            CtrlPressed               = true,
            CommandOrControlAutoremap = true,
        };
        _searchShortcut.Events.Add(shortcutEvent);
        SearchGuide();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !_searchShortcut.MatchesEvent(keyEvent) || !keyEvent.Pressed ||
            keyEvent.Echo)
        {
            return;
        }

        _currentTween.Complete();
        SetSearching(!_isSearching);
        GetViewport().SetInputAsHandled();
        AcceptEvent();
    }

    private void SetSearching(bool isSearching)
    {
        var toPosition = isSearching ? new Vector2I(0, 0) : new Vector2I(0, 70);
        GTweenGodotExtensions.Tween(GetOffsetTransformPosition, SetOffsetTransformPosition, toPosition,
            _tweenDuration).SetEasing(Easing.InOutBack).OnComplete(() => _searchBar.Clear()).Play();
        _isSearching = isSearching;
        if (isSearching)
        {
            Callable.From(() => _searchBar.GrabFocus()).CallDeferred();
        }
        else
        {
            _searchBar.ReleaseFocus();
        }
    }

    //Shows the search bar temporarily on start, just to let you know it exists
    private void SearchGuide()
    {
        var initialText = _searchBar.GetPlaceholder();
        _searchBar.SetPlaceholder("Ctrl+F to open search");
        _currentTween = GTweenSequenceBuilder.New().Append(GTweenGodotExtensions.Tween(GetOffsetTransformPosition,
                SetOffsetTransformPosition, new Vector2(0, 0),
                _tweenDuration).SetEasing(Easing.InOutBack))
            .AppendTime(2)
            .Append(GTweenGodotExtensions.Tween(GetOffsetTransformPosition,
                SetOffsetTransformPosition, new Vector2(0, 70),
                _tweenDuration).SetEasing(Easing.InOutBack).OnComplete(() => _searchBar.SetPlaceholder(initialText)))
            .Build();
        _currentTween.Play();
    }

    private void SearchBarOnTextChanged(string newText)
    {
        EmitSignalOnSearchBarUpdated(newText);
        RefreshColors(newText);
    }

    private void RefreshColors(string newText)
    {
        _richSearchBar.Clear();
        if (newText.IsNullOrEmpty())
        {
            return;
        }

        var parts = newText.Split(" ");
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            if (part.StartsWith('@'))
            {
                _richSearchBar.PushColor(Color.FromString("#CAA5F6", Colors.LightPink));
                _richSearchBar.AddText(part);
                _richSearchBar.Pop();
            }
            else
            {
                _richSearchBar.PushColor(Colors.White);
                _richSearchBar.AddText(part);
                _richSearchBar.Pop();
            }

            if (i < parts.Length - 1)
            {
                _richSearchBar.AddText(" ");
            }
        }
    }
}