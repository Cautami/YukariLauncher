using Godot;
using System;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using GTweens.Builders;
using GTweens.Easings;
using GTweens.Extensions;
using GTweens.Tweens;
using GTweensGodot.Extensions;
using NexusMods.Paths;
using YukariLauncher;
using YukariLauncher.Config;

public partial class Yukari : Node
{
    public static Yukari Instance { get; private set; }
    [Export] private GameContainer _gameContainer;
    [Export] private TextureRect _backgroundTexture;
    [Export] private float _tweenDuration;

    public static StringName UserPath => ProjectSettings.GlobalizePath("user://");
    public static StringName DefaultDownloadPath => ProjectSettings.GlobalizePath("user://.dlcache");

    public static StringName DefaultInstallPath => ProjectSettings.GlobalizePath("user://games");

    public event Action AppStarted;

    public event Action AppClosed;
    private static readonly float ScaleA = (float)(0.5 / Math.Log(5));
    private static readonly float ScaleB = (float)(1.0 - ScaleA * Math.Log(1152));

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance                   =  this;
        _gameContainer.CardHovered += GameContainerOnCardHovered;
        UIScale                    =  ScaleA * (float)Math.Log(DisplayServer.ScreenGetSize().X) + ScaleB;
        GetWindow().SetSize(new Vector2I((int)(GetWindow().GetSize().X * UIScale), (int)(GetWindow().GetSize().Y *
            UIScale)));
        GetWindow().MoveToCenter();
    }

    private void GameContainerOnCardHovered(GameEntryResource gameEntry)
    {
        if (gameEntry.BannerArt is null)
        {
            return;
        }

        CrossfadeBackground(gameEntry.BannerArt);
    }

    private GTween _bgTween;

    private void CrossfadeBackground(Texture2D newTexture)
    {
        var bgMaterial = _backgroundTexture.GetMaterial() as ShaderMaterial;
        if (bgMaterial is null)
        {
            return;
        }

        _bgTween?.Kill();
        var currentBlend = bgMaterial.GetShaderParameter("blend").AsSingle();
        if (currentBlend > 0f)
        {
            if (currentBlend >= 0.5f)
            {
                _backgroundTexture.SetTexture((Texture2D)bgMaterial.GetShaderParameter("next_texture"));
            }

            bgMaterial.SetShaderParameter("blend", 0.0f);
        }

        bgMaterial.SetShaderParameter("next_texture", newTexture);
        var _bgCrossfadeTween = GTweenExtensions.Tween(
                () => bgMaterial.GetShaderParameter("blend").AsSingle(),
                x => bgMaterial.SetShaderParameter("blend", x),
                1.0f, _tweenDuration)
            .OnComplete(() =>
            {
                _backgroundTexture.SetTexture(newTexture);
                bgMaterial.SetShaderParameter("next_texture", newTexture);
                bgMaterial.SetShaderParameter("blend", 0.0f);
            });
        _bgTween = GTweenSequenceBuilder.New()
            .Append(_backgroundTexture.TweenSelfModulate(Colors.White, _tweenDuration))
            .Join(_bgCrossfadeTween)
            .Build();
        _bgTween.SetEasing(Easing.InOutExpo);
        _bgTween.Play();
    }


    [Export(PropertyHint.Range, "1, 1.5f")]
    private float UIScale { get; set; } = 1f;

    public override void _Ready()
    {
        AppStarted?.Invoke();

        var steamGameLocator =
            new SteamHandler(FileSystem.Shared, OperatingSystem.IsWindows() ? WindowsRegistry.Shared : null);
        var gameinfo = steamGameLocator.FindOneGameById(AppId.From(3675420), out var errors);
        if (gameinfo == null)
        {
            return;
        }

        _backgroundTexture.SetSelfModulate(Colors.Transparent);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        GetWindow().SetContentScaleFactor(UIScale);
    }


    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationWMCloseRequest)
        {
            AppClosed?.Invoke();
        }
    }
}