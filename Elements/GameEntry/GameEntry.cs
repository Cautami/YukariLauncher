using Godot;
using System;
using System.IO;
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

    [Export(PropertyHint.Range, "0.15, 0.45, 0.05")]
    private float _flipDuration = 0.15f;

    private GTween _hoverTween;
    private GTween _pressTween;
    private GTween _flipTween;

    [Export] private Label _lastPlayedLabel;
    [Export] private Label _playTimeLabel;

    [Export] private TextureRect _playIcon;

    public event Action<GameEntry> CardFlipped;

    [Export] private ProgressBar _downloadProgressBar;
    [Export] private Label _downloadProgressLabel;

    [Export] private Button _configButton;

    private bool _isInstalled;
    private bool _isHovered;
    private bool _isDownloading;
    private bool _isPlaying;
    private bool _isSteam;
    public bool IsFlipped;

    public override void _Ready()
    {
        base._Ready();
        Initialize();
        if (YukariConfig.Instance.ConfigData.DownloadableGamesCache.Any())
        {
            RefreshPlayIcon();
        }

        Ran.Instance.GameListRetrieved += RefreshPlayIcon;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Ran.Instance.GameListRetrieved -= RefreshPlayIcon;
        Ran.Instance.DownloadRequested -= RanOnDownloadRequested;
        Ran.Instance.DownloadProgress  -= RanOnDownloadProgress;
        Ran.Instance.DownloadCompleted -= RanOnDownloadCompleted;
        Ran.Instance.DownloadFailed    -= RanOnDownloadFailed;
        Ran.Instance.InstallComplete   -= RanOnInstallComplete;
        Chen.Instance.GameDetected     -= ChenOnGameDetected;
        Chen.Instance.GameClosed       -= ChenOnGameClosed;
        Chen.Instance.GameStarted      -= ChenOnGameStarted;
    }

    //Should the following unique name hell just be exports? the answer may shock you. 
    private void Initialize()
    {
        if (EntryResource is null)
        {
            GD.PrintErr($"EntryResource for {Name} is null");
            return;
        }

        var frontSide = GetNode<Control>("%FrontSide");
        var backSide = GetNode<Control>("%BackSide");

        Callable.From(() => frontSide.SetMouseFilter(MouseFilterEnum.Ignore)).CallDeferred();
        Callable.From(() => backSide.SetMouseFilter(MouseFilterEnum.Ignore)).CallDeferred();
        IsInstalled();
        SetLastPlayed();
        SetPlayTime();
        RefreshCardState();

        var idLabel = GetNode<Label>("%GameIdLabel");
        idLabel.SetText(EntryResource.Id.TrimPrefix("th"));
        var chronoLabel = GetNode<Label>("%GameChronologyLabel"); //label
        chronoLabel.SetText(EntryResource.Chronology.ToString());
        var backIdLabel = GetNode<Label>("%BackGameIdLabel");
        backIdLabel.SetText(EntryResource.Id.TrimPrefix("th"));
        var backChronoLabel = GetNode<Label>("%BackGameChronologyLabel"); //label
        backChronoLabel.SetText(EntryResource.Chronology.ToString());
        var typeLabel = GetNode<RichTextLabel>("%GameType"); //label
        typeLabel.SetText("[i]" + "[b]" + EntryResource.GetGameTypeName());

        var gameLabel = GetNode<RichTextLabel>("%GameName");
        gameLabel.SetText($"[b]{EntryResource.Name}[/b]");

        var gameInfo = GetNode<Control>("%GameInfo");
        gameInfo.SetModulate(Colors.Transparent);

        var diffStarContainer = GetNode("%DiffStarContainer");
        foreach (var child in diffStarContainer.GetChildren())
        {
            child.QueueFree();
        }

        _configButton.SetVisible(true);
        if (EntryResource.PatcherConfigExeName.IsNullOrEmpty() && !EntryResource.IsNativeConfig)
        {
            _configButton.SetVisible(false);
        }

        //TODO: stars should actually be based on difficulty completion, currently like this for visualization
        // var diffCompleted = (int)YukariConfig.GetGameHighestDifficulty(EntryResource.Id);
        // for (var i = 0; i < Random.Shared.NextInt64(0, 5); i++)
        // {
        //     var diffStar = new TextureRect();
        //     diffStar.SetTexture(GD.Load<Texture2D>("uid://c0h4hreynl6a4")); // DifficultyStar.svg
        //     diffStar.SetCustomMinimumSize(new Vector2(37.5f, 37.5f));
        //     diffStar.SetExpandMode(TextureRect.ExpandModeEnum.IgnoreSize);
        //     diffStar.SetStretchMode(TextureRect.StretchModeEnum.KeepAspectCentered);
        //     diffStar.SetLightMask(2);
        //     diffStar.SetOffsetTransformEnabled(true);
        //     diffStar.SetOffsetTransformPosition(new Vector2(0, 38));
        //     diffStar.SetModulate(Colors.Transparent);
        //     diffStarContainer.AddChild(diffStar);
        //     if (i != 3)
        //     {
        //         continue;
        //     }
        //     var gradientTexture = GetNode<PointLight2D>("%GradientLight").GetTexture() as GradientTexture2D;
        //     gradientTexture?.SetGradient(GD.Load<Gradient>("uid://c4k1endlfhmj3")); // ShinyGradient.tres
        // }

        //TODO: extra stage should be also based on extra stage completion, still just visualization
        // var extraStageStar = GetNode<Control>("%DifStarExtraStage");
        // extraStageStar.SetVisible(IsInstalled());
    }

    private void IsInstalled()
    {
        _isInstalled = Chen.IsGameInstalled(EntryResource, out var isSteam);
        _isSteam     = isSteam;
    }

    private void Refresh()
    {
        IsInstalled();
        SetPlayTime();
        SetLastPlayed();
        RefreshCardState();
        RefreshPlayIcon();
    }


    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (!IsFlipped && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                OnLeftClick();
                AcceptEvent();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
            {
                OnRightClick();
                AcceptEvent();
            }
        }

        base._GuiInput(@event);
    }

    private void OnLeftClick()
    {
        if (_isDownloading)
        {
            return;
        }

        if (IsFlipped)
        {
            return;
        }

        if (_isPlaying)
        {
            Chen.Instance.StopGame(EntryResource.Id);
            PressTween();
            return;
        }

        Refresh();
        if (!_isInstalled)
        {
            if (YukariConfig.IsGameDownloadable(EntryResource.Id))
            {
                PressTween();
                Ran.Instance.DownloadRequested += RanOnDownloadRequested;
                Ran.Instance.DownloadProgress  += RanOnDownloadProgress;
                Ran.Instance.DownloadCompleted += RanOnDownloadCompleted;
                Ran.Instance.InstallComplete   += RanOnInstallComplete;
                Ran.Instance.DownloadFailed    += RanOnDownloadFailed;
                Ran.Instance.DownloadGame(EntryResource);
            }
            else if (EntryResource.SteamAppId != 0)
            {
                PressTween();
                OS.ShellOpen($"steam://openurl/https://store.steampowered.com/app/{EntryResource.SteamAppId}/");
            }
        }
        else
        {
            PressTween();
            Chen.Instance.GameDetected += ChenOnGameDetected;
            Chen.Instance.GameClosed   += ChenOnGameClosed;
            Chen.Instance.GameStarted  += ChenOnGameStarted;

            _ = Chen.Instance.StartGame(EntryResource, _isSteam);
        }
    }

    private void OnRightClick()
    {
        if (!_isInstalled)
        {
            return;
        }

        FlipCard();
    }

    public void FlipCard()
    {
        if (_flipTween is not null && _flipTween.IsPlaying)
        {
            return;
        }

        var offsetScale = IsFlipped ? new Vector2(1, 1) : new Vector2(-1, 1);

        var zeroScaleTween = GTweenGodotExtensions.Tween(GetOffsetTransformScale, SetOffsetTransformScale,
                new Vector2(0, 1), _flipDuration / 2)
            .SetEasing(Easing.InExpo);
        var targetScaleTween = GTweenGodotExtensions
            .Tween(GetOffsetTransformScale, SetOffsetTransformScale, offsetScale, _flipDuration / 2)
            .SetEasing(Easing.OutExpo);
        _flipTween = GTweenSequenceBuilder.New()
            .Append(zeroScaleTween)
            .AppendCallback(() =>
            {
                if (!IsFlipped)
                {
                    GetNode<Control>("%FrontSide").SetVisible(false);
                    GetNode<Control>("%BackSide").SetVisible(true);
                    CardFlipped?.Invoke(this);
                }
                else
                {
                    GetNode<Control>("%FrontSide").SetVisible(true);
                    GetNode<Control>("%BackSide").SetVisible(false);
                }

                IsFlipped = !IsFlipped;
            })
            .Append(targetScaleTween)
            .Build();
        _flipTween.Play();
    }

    #region ChenSignals

    private void ChenOnGameStarted(GameEntryResource gameEntry)
    {
        if (gameEntry != EntryResource)
        {
            return;
        }

        _isPlaying = true;
        _playIcon.SetVisible(false);
        _downloadProgressBar.SetVisible(true);
        _downloadProgressBar.SetIndeterminate(true);
    }

    private void ChenOnGameDetected(GameEntryResource gameEntry)
    {
        if (gameEntry != EntryResource)
        {
            return;
        }

        _playIcon.SetVisible(true);

        _downloadProgressBar.SetVisible(false);
        _downloadProgressBar.SetIndeterminate(false);
        Refresh();
    }

    private void ChenOnGameClosed(GameEntryResource gameEntry)
    {
        if (gameEntry != EntryResource)
        {
            return;
        }

        _isPlaying = false;
        Refresh();
        Chen.Instance.GameDetected -= ChenOnGameDetected;
        Chen.Instance.GameClosed   -= ChenOnGameClosed;
        Chen.Instance.GameStarted  -= ChenOnGameStarted;
    }

    #endregion

    #region RanSignals

    private void RanOnDownloadRequested(string id)
    {
        if (id != EntryResource.Id)
        {
            return;
        }

        _downloadProgressBar.SetVisible(true);
        _downloadProgressBar.SetIndeterminate(true);
        _downloadProgressLabel.SetVisible(false);
        _playIcon.SetVisible(false);
        _isDownloading = true;
    }

    private void RanOnDownloadProgress(string id, float progress)
    {
        if (id != EntryResource.Id)
        {
            return;
        }


        _downloadProgressBar.SetIndeterminate(false);
        _downloadProgressBar.SetValue(progress);
        _downloadProgressLabel.SetVisible(true);
        _downloadProgressLabel.SetText($"{progress:N0}%");
    }

    private void RanOnDownloadCompleted(string id)
    {
        if (id != EntryResource.Id)
        {
            return;
        }

        _downloadProgressBar.SetIndeterminate(true);
        _downloadProgressLabel.SetVisible(false);
    }

    private void RanOnDownloadFailed(string id)
    {
        if (id != EntryResource.Id)
        {
            return;
        }

        _downloadProgressBar.SetVisible(false);
        _downloadProgressLabel.SetVisible(false);
        EndDownload();
    }

    private void RanOnInstallComplete(string id, string path)
    {
        if (id != EntryResource.Id)
        {
            return;
        }

        _downloadProgressBar.SetVisible(false);
        _downloadProgressLabel.SetVisible(false);

        YukariConfig.Instance.AddOrUpdateGameRecord(EntryResource, path);
        EndDownload();
    }

    private void EndDownload()
    {
        _playIcon.SetVisible(true);
        IsInstalled();

        RefreshCardState();
        RefreshPlayIcon();
        _isDownloading                 =  false;
        Ran.Instance.DownloadRequested -= RanOnDownloadRequested;
        Ran.Instance.DownloadCompleted -= RanOnDownloadCompleted;
        Ran.Instance.DownloadProgress  -= RanOnDownloadProgress;
        Ran.Instance.DownloadFailed    -= RanOnDownloadFailed;
        Ran.Instance.InstallComplete   -= RanOnInstallComplete;
    }

    #endregion

    private void PressTween()
    {
        _pressTween?.Kill();
        var tweenScaleIn = GTweenGodotExtensions.Tween(GetOffsetTransformScale,
            SetOffsetTransformScale, new Vector2(0.9f, 0.9f), _pressDuration);
        var tweenRotationIn = GTweenExtensions.Tween(GetOffsetTransformRotation,
            SetOffsetTransformRotation, Mathf.DegToRad(Random.Shared.NextInt64(-3, 3)), _pressDuration);

        var tweenScaleOut = GTweenGodotExtensions.Tween(GetOffsetTransformScale,
            SetOffsetTransformScale, new Vector2(1f, 1f), _pressDuration);
        var tweenRotationOut = GTweenExtensions.Tween(GetOffsetTransformRotation,
            SetOffsetTransformRotation, Mathf.DegToRad(0), _pressDuration);

        _pressTween = GTweenSequenceBuilder.New()
            .Append(tweenScaleIn)
            .Join(tweenRotationIn)
            .Append(tweenScaleOut)
            .Join(tweenRotationOut)
            .Build();

        _pressTween.SetEasing(Easing.InOutBack);
        _pressTween.Play();
    }

    private void OnMouseEntered()
    {
        _hoverTween?.Kill();
        _isHovered = true;
        var cardImage = GetNode<TextureRect>("%CardImage");
        var gameInfo = GetNode<Control>("%GameInfo");

        var tweenCardImageColor = cardImage.TweenSelfModulate(Color.FromString("464646", Colors.White),
            _hoverDuration);
        var tweenCardScale = GTweenGodotExtensions.Tween(cardImage.GetOffsetTransformScale,
            cardImage.SetOffsetTransformScale, new Vector2(1.1f, 1.1f), _hoverDuration);
        var tweenCardRotation = GTweenExtensions.Tween(cardImage.GetOffsetTransformRotation,
            cardImage.SetOffsetTransformRotation, Mathf.DegToRad(4), _hoverDuration);

        var tweenGameInfoAlpha = gameInfo.TweenModulateAlpha(1, _hoverDuration);

        var tweenPlayIconAlpha = _playIcon.TweenSelfModulateAlpha(1, _hoverDuration);

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

        _hoverTween = tweenSequenceBuilder.Build();
        _hoverTween.Play();
    }

    private void OnMouseExited()
    {
        _hoverTween?.Kill();
        _isHovered = false;
        var cardImage = GetNode<TextureRect>("%CardImage");
        var gameInfo = GetNode<Control>("%GameInfo");

        var tweenCardImageColor =
            cardImage.TweenSelfModulate(Colors.White,
                _hoverDuration);
        var tweenCardScale = GTweenGodotExtensions.Tween(cardImage.GetOffsetTransformScale,
            cardImage.SetOffsetTransformScale, new Vector2(1, 1), _hoverDuration);
        var tweenCardRotation = GTweenExtensions.Tween(cardImage.GetOffsetTransformRotation,
            cardImage.SetOffsetTransformRotation, Mathf.DegToRad(0), _hoverDuration);

        var tweenGameInfoAlpha = gameInfo.TweenModulateAlpha(0, _hoverDuration);

        var tweenPlayIconAlpha = _playIcon.TweenSelfModulateAlpha(0, _hoverDuration);

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

        _hoverTween = tweenSequenceBuilder.Build();

        _hoverTween.Play();
    }

    private void SetLastPlayed()
    {
        if (_lastPlayedLabel is null)
        {
            return;
        }

        var lastPlayedDateTime = YukariConfig.GetGameLastPlayed(EntryResource.Id).ToLocalTime();
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
        else if (playTime.TotalMinutes < 60)
        {
            _playTimeLabel.SetText($"{(int)playTime.TotalMinutes} minutes");
        }
        else
        {
            _playTimeLabel.SetText($"{(int)playTime.TotalHours:N0} hours");
        }
    }

    private void RefreshCardState()
    {
        var cardImage = GetNode<TextureRect>("%CardImage");
        var backCardImage = GetNode<TextureRect>("%BackCardImage");
        cardImage.SetTexture(EntryResource.CoverArt);
        backCardImage.SetTexture(EntryResource.CoverArt);
        cardImage.SetModulate(_isInstalled ? Colors.White : Color.FromString("#777777", Colors.HotPink));
        var cardImageMaterial = cardImage.GetMaterial() as ShaderMaterial;
        var cardImageSaturation = (float)(_isInstalled ? 1 : 0);
        cardImageMaterial?.SetShaderParameter("saturation", cardImageSaturation);

        _downloadProgressBar.SetVisible(false);
        _downloadProgressLabel.SetVisible(false);
    }

    private void RefreshPlayIcon()
    {
        _playIcon.SetTexture(_isInstalled
            ? GD.Load<Texture2D>("uid://o2pc5jihbtxl") //Play.svg
            : GD.Load<Texture2D>("uid://b1ibyb8h5cpog")); //Download.svg
        _playIcon.SetModulate(_isInstalled
            ? Color.FromString("#89D88B", Colors.HotPink)
            : Color.FromString("#74A8FC", Colors.DeepPink));
        switch (_isInstalled)
        {
            case true when _isPlaying:
                _playIcon.SetTexture(GD.Load<Texture2D>("uid://btnwmx7ruj2o3")); //stop.svg
                _playIcon.SetModulate(Color.FromString("F38BA8", Colors.Yellow));
                break;
            case false when EntryResource.SteamAppId != 0 && !YukariConfig.IsGameDownloadable(EntryResource.Id):
                _playIcon.SetTexture(GD.Load<Texture2D>("uid://ccjcn4mt34taj")); //Steam.svg;
                break;
            case false when !YukariConfig.IsGameDownloadable(EntryResource.Id):
                _playIcon.SetTexture(GD.Load<Texture2D>("uid://bud0vqmjbsnjs")); //download_off.svg
                _playIcon.SetModulate(Color.FromString("#F38BA8", Colors.Yellow));
                break;
        }

        if (!_isHovered)
        {
            _playIcon.SetSelfModulate(Colors.Transparent);
        }
    }

    //TODO: Split all of this up, this class is becoming a monolith, I'd assume a FrontSide.cs and BackSide.cs would be a start.

    #region BackSideSignals

    private bool _uninstallConfirming = false;
    private GTween _uninstallTween;

    private void OnUninstallPressed()
    {
        var confirmation = GetNode<HBoxContainer>("%UninstallConfirmation");
        var uninstallButton = GetNode<Button>("%Uninstall");
        if (confirmation is null)
        {
            return;
        }

        _uninstallTween?.Complete();
        if (!_uninstallConfirming)
        {
            confirmation.SetVisible(true);
            uninstallButton.SetDisabled(true);
            var confirmPositionTween = GTweenGodotExtensions
                .Tween(confirmation.GetOffsetTransformPosition, confirmation.SetOffsetTransformPosition,
                    new Vector2(0, 42),
                    0.3f).SetEasing(Easing.InOutBack).OnComplete(() => uninstallButton.SetDisabled(false));
            var confirmAlphaTween = confirmation.TweenModulateAlpha(1, 0.3f).SetEasing(Easing.InOutBack);
            _uninstallTween = GTweenSequenceBuilder.New().Append(confirmPositionTween).Join(confirmAlphaTween).Build();

            _uninstallTween.Play();
            _uninstallConfirming = true;
        }
        else
        {
            var confirmPositionTween = GTweenGodotExtensions
                .Tween(confirmation.GetOffsetTransformPosition, confirmation.SetOffsetTransformPosition,
                    new Vector2(0, 12),
                    0.3f).SetEasing(Easing.InOutBack).OnComplete(() => confirmation.SetVisible(false));
            var confirmAlphaTween = confirmation.TweenModulateAlpha(0, 0.3f).SetEasing(Easing.InOutBack);
            _uninstallTween = GTweenSequenceBuilder.New().Append(confirmPositionTween).Join(confirmAlphaTween).Build();

            _uninstallTween.Play();
            _uninstallConfirming = false;
        }
    }

    private void OnUninstallConfirmPressed()
    {
        GD.Print("Confirmed pressed");
        Chen.UninstallGame(EntryResource);
        FlipCard();
        Refresh();
    }

    private void OnOpenSettingsPressed()
    {
        Chen.Instance.OpenSettings(EntryResource);
    }

    private void OnLocalFilesPressed()
    {
        Chen.BrowseFiles(EntryResource.Id);
    }

    #endregion
}