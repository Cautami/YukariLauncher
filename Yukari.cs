using Godot;
using System;
using GTweens.Builders;
using GTweens.Easings;
using GTweens.Extensions;
using GTweens.Tweens;
using GTweensGodot.Extensions;
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

    public override void _EnterTree()
    {
        Instance = this;

        _gameContainer.CardHovered += GameContainerOnCardHovered;

        GetWindow().SetSize(YukariConfig.Instance.ConfigData.ScreenSize);
        GetWindow().SetMode(YukariConfig.Instance.ConfigData.Maximized
            ? Window.ModeEnum.Maximized
            : Window.ModeEnum.Windowed);
        SetUiScale(YukariConfig.Instance.ConfigData.UiScale);
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
        var bgCrossfadeTween = GTweenExtensions.Tween(
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
            .Join(bgCrossfadeTween)
            .Build();
        _bgTween.SetEasing(Easing.InOutExpo);
        _bgTween.Play();
    }

    public override void _Ready()
    {
        AppStarted?.Invoke();

        _backgroundTexture.SetSelfModulate(Colors.Transparent);
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what != NotificationWMCloseRequest)
        {
            return;
        }

        AppClosed?.Invoke();
    }

    public void SetUiScale(float scale)
    {
        GetWindow().SetContentScaleFactor(scale);
    }

    public void SetGridColumns(int columns)
    {
        _gameContainer.SetColumns(columns);
    }
}