using Godot;
using System;
using System.Linq;
using GTweens.Builders;
using GTweens.Easings;
using GTweens.Extensions;
using GTweens.Tweens;
using GTweensGodot.Extensions;
using YukariApp.GameTracker;
using YukariLauncher;
using YukariLauncher.Config;

public partial class GameEntry : Control
{
    [Export] public GameEntryResource EntryResource { get; set; }

    [Export(PropertyHint.Range, "0.15, 0.45, 0.05")]
    private float _hoverDuration = 0.15f;

    [Export(PropertyHint.Range, "0.15, 0.45, 0.05")]
    private float _pressDuration = 0.15f;

    [Export] private Label _lastPlayedLabel;
    [Export] private Label _playTimeLabel;


    private bool _isInstalled;
    private bool _isHovered;

    public override void _Ready()
    {
        base._Ready();
        Initialize();
        Chen.Instance.GameStarted += OnGameStarted;
        Chen.Instance.GameUpdated += OnGameUpdated;
        Chen.Instance.GameClosed  += OnGameClosed;

        Ran.Instance.GameRequested += OnGameRequested;
        Ran.Instance.GameInstalled += OnGameInstalled;
    }

//Should the following unique name hell just be exports? the answer may shock you. 
    private void Initialize()
    {
        if (EntryResource is null)
        {
            GD.PrintErr($"EntryResource for {Name} is null");
            return;
        }

        _isInstalled = IsInstalled();
        SetLastPlayed();
        SetPlayTime();

        var idLabel = GetNode<Label>("%GameIdLabel");
        idLabel.SetText(EntryResource.Id.TrimPrefix("th"));
        var typeLabel = GetNode<Label>("%GameTypeLabel"); //label
        typeLabel.SetText(EntryResource.Type.ToString());

        var cardImage = GetNode<TextureRect>("%CardImage");
        cardImage.SetTexture(EntryResource.CoverArt);
        cardImage.SetModulate(_isInstalled ? Colors.White : Color.FromString("#777777", Colors.HotPink));
        var cardImageMaterial = cardImage.GetMaterial() as ShaderMaterial;
        var cardImageSaturation = (float)(_isInstalled ? 1 : 0);
        cardImageMaterial?.SetShaderParameter("saturation", cardImageSaturation);

        var gameLabel = GetNode<RichTextLabel>("%GameName");
        gameLabel.SetText($"[b]{EntryResource.Name}[/b]");

        var gameInfo = GetNode<Control>("%GameInfo");
        gameInfo.SetModulate(Colors.Transparent);

        var diffStarContainer = GetNode("%DiffStarContainer");
        foreach (var child in diffStarContainer.GetChildren())
        {
            child.QueueFree();
        }

        //TODO: stars should actually be based on difficulty completion, currently like this for visualization
        var diffCompleted = (int)YukariConfig.GetGameHighestDifficulty(EntryResource.Id);
        for (var i = 0; i < Random.Shared.NextInt64(0, 5); i++)
        {
            var diffStar = new TextureRect();
            diffStar.SetTexture(GD.Load<Texture2D>("uid://c0h4hreynl6a4")); // DifficultyStar.svg
            diffStar.SetCustomMinimumSize(new Vector2(37.5f, 37.5f));
            diffStar.SetExpandMode(TextureRect.ExpandModeEnum.IgnoreSize);
            diffStar.SetStretchMode(TextureRect.StretchModeEnum.KeepAspectCentered);
            diffStar.SetLightMask(2);
            diffStar.SetOffsetTransformEnabled(true);
            diffStar.SetOffsetTransformPosition(new Vector2(0, 38));
            diffStar.SetModulate(Colors.Transparent);
            diffStarContainer.AddChild(diffStar);
            if (i != 3)
            {
                continue;
            }

            var gradientTexture = GetNode<PointLight2D>("%GradientLight").GetTexture() as GradientTexture2D;
            gradientTexture?.SetGradient(GD.Load<Gradient>("uid://c4k1endlfhmj3")); // ShinyGradient.tres
        }

        var extraStageStar = GetNode<Control>("%DifStarExtraStage");
        extraStageStar.SetVisible(IsInstalled());

        var playIcon = GetNode<TextureRect>("%PlayIcon");
        playIcon.SetTexture(_isInstalled
            ? GD.Load<Texture2D>("uid://o2pc5jihbtxl")
            : GD.Load<Texture2D>("uid://b1ibyb8h5cpog")); //Play.svg | Download.svg
        playIcon.SetModulate(_isInstalled
            ? Color.FromString("#89D88B", Colors.HotPink)
            : Color.FromString("#74A8FC", Colors.DeepPink));
        playIcon.SetSelfModulate(_isInstalled ? Colors.Transparent : Colors.White);
    }

    //Testing bool
    private bool IsInstalled()
    {
        if (YukariConfig.GetGameInstallPath(EntryResource.Id).IsNullOrEmpty())
        {
            return false;
        }

        return true;
    }

    private void Refresh()
    {
        _isInstalled = IsInstalled();
        SetPlayTime();
        SetLastPlayed();
    }

    private void OnGameStarted(GameEntryResource entryResource)
    {
        CallDeferred(nameof(Refresh));
    }

    private void OnGameUpdated(GameEntryResource entryResource)
    {
        CallDeferred(nameof(Refresh));
    }

    private void OnGameClosed(GameEntryResource entryResource)
    {
        CallDeferred(nameof(Refresh));
    }

    private void OnGameRequested(GameEntryResource entryResource) { }
    private void OnGameInstalled(GameEntryResource entryResource) { }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);
        if (@event is not InputEventMouseButton mouseEvent)
        {
            return;
        }

        if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            OnLeftClick();
        }
    }

    private void OnLeftClick()
    {
        PressTween();

        GD.Print(_isInstalled);
        if (!_isInstalled)
        {
            Ran.Instance.DownloadGame(EntryResource);
        }
        else
        {
            Chen.Instance.StartGame(EntryResource);
        }
    }

    private void PressTween()
    {
        var tweenScaleIn = GTweenGodotExtensions.Tween(GetOffsetTransformScale,
            SetOffsetTransformScale, new Vector2(0.9f, 0.9f), _pressDuration);
        var tweenRotationIn = GTweenExtensions.Tween(GetOffsetTransformRotation,
            SetOffsetTransformRotation, Mathf.DegToRad(2), _pressDuration);

        var tweenScaleOut = GTweenGodotExtensions.Tween(GetOffsetTransformScale,
            SetOffsetTransformScale, new Vector2(1f, 1f), _pressDuration);
        var tweenRotationOut = GTweenExtensions.Tween(GetOffsetTransformRotation,
            SetOffsetTransformRotation, Mathf.DegToRad(0), _pressDuration);

        var tween = GTweenSequenceBuilder.New()
            .Append(tweenScaleIn)
            .Join(tweenRotationIn)
            .Append(tweenScaleOut)
            .Join(tweenRotationOut)
            .Build();

        tween.SetEasing(Easing.InOutBack);
        tween.Play();
    }

    private void OnMouseEntered()
    {
        var cardImage = GetNode<TextureRect>("%CardImage");
        var gameInfo = GetNode<Control>("%GameInfo");
        var playIcon = GetNode<TextureRect>("%PlayIcon");

        var tweenCardImageColor = cardImage.TweenModulate(Color.FromString("464646", Colors.White),
            _hoverDuration);
        var tweenCardScale = GTweenGodotExtensions.Tween(cardImage.GetOffsetTransformScale,
            cardImage.SetOffsetTransformScale, new Vector2(1.1f, 1.1f), _hoverDuration);
        var tweenCardRotation = GTweenExtensions.Tween(cardImage.GetOffsetTransformRotation,
            cardImage.SetOffsetTransformRotation, Mathf.DegToRad(4), _hoverDuration);

        var tweenGameInfoAlpha = gameInfo.TweenModulateAlpha(1, _hoverDuration);

        var tweenPlayIconAlpha = playIcon.TweenSelfModulateAlpha(1, _hoverDuration);

        var tweenSequenceBuilder = GTweenSequenceBuilder.New()
            .Append(tweenCardScale)
            .Join(tweenCardRotation)
            .Join(tweenCardImageColor)
            .Join(tweenGameInfoAlpha)
            .Join(tweenPlayIconAlpha);
        foreach (var child in GetNode<HBoxContainer>("%DiffStarContainer").GetChildren())
        {
            if (child is not TextureRect star)
            {
                continue;
            }

            var tweenStarSlide = GTweenGodotExtensions.Tween(star.GetOffsetTransformPosition,
                star.SetOffsetTransformPosition, new Vector2(0, 0), _hoverDuration).SetEasing(Easing.InOutBack);
            var tweenStarAlpha = star.TweenModulateAlpha(1, _hoverDuration).SetEasing(Easing.InOutBack);

            tweenSequenceBuilder.Join(tweenStarAlpha);
            tweenSequenceBuilder.Join(tweenStarSlide);
        }

        var tweenSequence = tweenSequenceBuilder.Build();

        tweenSequence.Play();
    }

    private void OnMouseExited()
    {
        var cardImage = GetNode<TextureRect>("%CardImage");
        var gameInfo = GetNode<Control>("%GameInfo");
        var playIcon = GetNode<TextureRect>("%PlayIcon");

        var tweenCardImageColor =
            cardImage.TweenModulate(_isInstalled ? Colors.White : Color.FromString("777777", Colors.DeepPink),
                _hoverDuration);
        var tweenCardScale = GTweenGodotExtensions.Tween(cardImage.GetOffsetTransformScale,
            cardImage.SetOffsetTransformScale, new Vector2(1, 1), _hoverDuration);
        var tweenCardRotation = GTweenExtensions.Tween(cardImage.GetOffsetTransformRotation,
            cardImage.SetOffsetTransformRotation, Mathf.DegToRad(0), _hoverDuration);

        var tweenGameInfoAlpha = gameInfo.TweenModulateAlpha(0, _hoverDuration);

        var tweenPlayIconAlpha = playIcon.TweenSelfModulateAlpha(_isInstalled ? 0 : 1, _hoverDuration);

        var tweenSequenceBuilder = GTweenSequenceBuilder.New()
            .Append(tweenCardScale)
            .Join(tweenCardRotation)
            .Join(tweenCardImageColor)
            .Join(tweenGameInfoAlpha)
            .Join(tweenPlayIconAlpha);
        foreach (var child in GetNode<HBoxContainer>("%DiffStarContainer").GetChildren())
        {
            if (child is not TextureRect star)
            {
                continue;
            }

            var tweenStarSlide = GTweenGodotExtensions.Tween(star.GetOffsetTransformPosition,
                star.SetOffsetTransformPosition, new Vector2(0, 38), _hoverDuration).SetEasing(Easing.InOutBack);
            var tweenStarAlpha = star.TweenModulateAlpha(0, _hoverDuration).SetEasing(Easing.InOutBack);

            tweenSequenceBuilder.Join(tweenStarAlpha);
            tweenSequenceBuilder.Join(tweenStarSlide);
        }

        var tweenSequence = tweenSequenceBuilder.Build();

        tweenSequence.Play();
    }

    private void SetLastPlayed()
    {
        if (_lastPlayedLabel is null)
        {
            return;
        }

        var lastPlayedDateTime = YukariConfig.GetGameLastPlayed(EntryResource.Id);
        if (lastPlayedDateTime == DateTime.MinValue)
        {
            _lastPlayedLabel.SetText("Never");
        }
        else if (lastPlayedDateTime.Date == DateTime.Today)
        {
            _lastPlayedLabel.SetText("Today");
        }
        else if (lastPlayedDateTime.Date == DateTime.Today.AddDays(-1))
        {
            _lastPlayedLabel.SetText("Yesterday");
        }
        else
        {
            _lastPlayedLabel.SetText($"{lastPlayedDateTime:MMM d}");
        }
    }

    private void SetPlayTime()
    {
        var playTimeContainer = GetNode<Panel>("%TimePlayed");
        if (_playTimeLabel is null || playTimeContainer is null)
        {
            return;
        }

        playTimeContainer.SetVisible(true);
        var playTime = TimeSpan.FromSeconds(YukariConfig.GetGamePlayTime(EntryResource.Id));
        if (playTime.TotalSeconds == 0)
        {
            playTimeContainer.SetVisible(false);
        }

        if (playTime.TotalSeconds < 60)
        {
            _playTimeLabel.SetText("<1 minute");
        }
        else if (playTime.Minutes < 60)
        {
            _playTimeLabel.SetText($"{(int)playTime.TotalMinutes} minutes");
        }
        else
        {
            _playTimeLabel.SetText($"{(int)playTime.TotalHours:N0} hours");
        }
    }
}