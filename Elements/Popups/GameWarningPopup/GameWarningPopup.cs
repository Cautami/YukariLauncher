using Godot;
using System;
using System.IO;
using GTweens.Builders;
using GTweens.Easings;
using GTweens.Tweens;
using GTweensGodot.Extensions;
using YukariLauncher;
using YukariLauncher.Config;

public partial class GameWarningPopup : Control
{
    [Export] private Label _installLabel;

    [Export] private Button _cancelButton;
    [Export] private Button _confirmButton;

    [Export] private float _popupTweenDuration = 0.5f;
    [Export] private float _buttonTweenDuration = 0.25f;

    public event Action<bool> PromptConfirmed;

    private void TweenPopup()
    {
        var popup = GetNode<Panel>("%Popup");
        var darken = GetNode<Panel>("%Darken");

        popup.SetModulate(Colors.Transparent);
        popup.SetOffsetTransformPosition(new Vector2(0, 100));
        darken.SetModulate(Colors.Transparent);

        var tweenBuilder = GTweenSequenceBuilder.New()
            .Append(GTweenGodotExtensions.Tween(popup.GetOffsetTransformPosition, popup.SetOffsetTransformPosition,
                new Vector2(0, 0), _popupTweenDuration))
            .Join(popup.TweenModulate(Colors.White, _popupTweenDuration))
            .Join(darken.TweenModulate(Colors.White, _popupTweenDuration))
            .Build();
        tweenBuilder.SetEasing(Easing.InOutExpo);
        tweenBuilder.Play();
        TreeExited += tweenBuilder.Kill;
    }

    public override void _Ready()
    {
        base._Ready();
        TweenPopup();
        _installLabel.SetText($"Continuing will close all running games.");
    }

    private void OnCancelPressed()
    {
        PromptConfirmed?.Invoke(false);
        QueueFree();
    }

    private void OnConfirmPressed()
    {
        PromptConfirmed?.Invoke(true);
        QueueFree();
    }
}