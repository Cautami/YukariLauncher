using Godot;
using System;
using System.Text.RegularExpressions;
using GTweens.Easings;
using GTweensGodot.Extensions;
using YukariApp.GameTracker;
using YukariLauncher.Config;

public partial class SettingsMenu : Control
{
    //Opposed to GetNode hell, this time I'm grabbing references via export.
    //My rationale for not doing this prior was cluttering the inspector, but perhaps it was irrational. 
    [Export] private float _tweenDuration;

    [Export] private Button _settingsButton;
    [Export] private Control _settingsContainer;

    [Export, ExportGroup("References")] private CheckButton _discordRpcCheckButton;
    [Export] private HSlider _uiScaleSlider;
    [Export] private HSlider _columnSlider;
    [Export] private LineEdit _apiLineEdit;
    [Export] private Label _uiScaleLabel;
    [Export] private Button _apiSaveButton;
    [Export] private Label _columnLabel;

    private bool _isSettingsOpened;

    //I'm also going to just connect the signals manually because I think it probably reads better.
    public override void _Ready()
    {
        base._Ready();
        _settingsButton.Pressed        += SettingsButtonOnPressed;
        _discordRpcCheckButton.Toggled += DiscordRpcCheckButtonOnToggled;
        _uiScaleSlider.ValueChanged    += UiScaleSliderOnValueChanged;
        _uiScaleSlider.DragEnded       += UiScaleSliderOnDragEnded;
        _apiLineEdit.TextChanged       += ApiLineEditOnTextChanged;
        _apiSaveButton.Pressed         += ApiSaveButtonOnPressed;
        _columnSlider.ValueChanged     += ColumnSliderOnValueChanged;

        _discordRpcCheckButton.SetPressed(YukariConfig.Instance.ConfigData.DiscordRpcEnabled);
        _uiScaleSlider.SetValue(YukariConfig.Instance.ConfigData.UiScale);
        _apiLineEdit.SetPlaceholder(ProjectSettings.GetSetting("application/config/default_ran_api").AsStringName());
        _apiLineEdit.SetText(YukariConfig.Instance.ConfigData.ApiAddress);
        _columnSlider.SetValue(YukariConfig.Instance.ConfigData.CardColumns);
    }

    //I'm like 90% sure you don't need to manually disconnect godot signals
    public override void _ExitTree()
    {
        base._ExitTree();
        _settingsButton.Pressed        -= SettingsButtonOnPressed;
        _discordRpcCheckButton.Toggled -= DiscordRpcCheckButtonOnToggled;
        _uiScaleSlider.ValueChanged    -= UiScaleSliderOnValueChanged;
        _uiScaleSlider.DragEnded       -= UiScaleSliderOnDragEnded;
        _apiLineEdit.TextChanged       -= ApiLineEditOnTextChanged;
        _apiSaveButton.Pressed         -= ApiSaveButtonOnPressed;
        _columnSlider.ValueChanged     -= ColumnSliderOnValueChanged;
    }

    private void SettingsButtonOnPressed()
    {
        if (!_isSettingsOpened)
        {
            GTweenGodotExtensions.Tween(_settingsContainer.GetOffsetTransformPosition,
                    _settingsContainer.SetOffsetTransformPosition, new Vector2(0, 0), _tweenDuration)
                .SetEasing(Easing.InOutCirc).Play();
        }
        else
        {
            GTweenGodotExtensions.Tween(_settingsContainer.GetOffsetTransformPosition,
                    _settingsContainer.SetOffsetTransformPosition, new Vector2(450, 0), _tweenDuration)
                .SetEasing(Easing.InOutCirc).Play();
        }

        _isSettingsOpened = !_isSettingsOpened;
    }

    private void DiscordRpcCheckButtonOnToggled(bool toggledOn)
    {
        YukariConfig.Instance?.ConfigData.DiscordRpcEnabled = toggledOn;
        DiscordRpc.Instance?.TogglePresence(toggledOn);
    }

    private void UiScaleSliderOnValueChanged(double value)
    {
        _uiScaleLabel.SetText($"{value:N2}x");
    }

    private void UiScaleSliderOnDragEnded(bool valueChanged)
    {
        if (valueChanged)
        {
            Yukari.Instance?.SetUiScale((float)_uiScaleSlider.Value);
            YukariConfig.Instance?.ConfigData.UiScale = (float)_uiScaleSlider.Value;
        }
    }

    private void ApiLineEditOnTextChanged(string newText)
    {
        GD.Print(newText);
        GD.Print(UrlRegex().IsMatch(newText));
        if (newText != YukariConfig.GetApiAddress() && UrlRegex().IsMatch(newText))
        {
            _apiSaveButton.SetDisabled(false);
        }
        else
        {
            _apiSaveButton.SetDisabled(true);
        }
    }

    private void ApiSaveButtonOnPressed()
    {
        YukariConfig.Instance?.ConfigData.ApiAddress = _apiLineEdit.GetText();
        _apiSaveButton.SetDisabled(true);
    }

    private void ColumnSliderOnValueChanged(double value)
    {
        _columnLabel.SetText($"{value:N0}");
        YukariConfig.Instance?.ConfigData.CardColumns = (int)value;
        Yukari.Instance?.SetGridColumns((int)value);
    }

    [GeneratedRegex(
        @"^https?://(?:www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b(?:[-a-zA-Z0-9()@:%_\+.~#?&/=]*)$")]
    private static partial Regex UrlRegex();
}