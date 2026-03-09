using Godot;
using System;
using System.IO;
using GTweens.Builders;
using GTweens.Easings;
using GTweensGodot.Extensions;
using YukariLauncher;
using YukariLauncher.Config;

public partial class InstallPopup : Control
{
    [Export] public GameEntryResource GameEntry;
    [Export] private LineEdit _pathEdit;
    [Export] private Label _installLabel;
    [Export] private Button _confirmButton;

    [Export] public bool IsLocatingProcess = false;


    private FileDialog _fileDialog;
    [Export] private float _tweenDuration = 0.5f;


    public event Action<string> PromptConfirmed;

    private void TweenPopup()
    {
        var popup = GetNode<Panel>("%Popup");
        var darken = GetNode<Panel>("%Darken");

        popup.SetModulate(Colors.Transparent);
        popup.SetOffsetTransformPosition(new Vector2(0, 100));
        darken.SetModulate(Colors.Transparent);

        var tweenBuilder = GTweenSequenceBuilder.New()
            .Append(GTweenGodotExtensions.Tween(popup.GetOffsetTransformPosition, popup.SetOffsetTransformPosition,
                new Vector2(0, 0), _tweenDuration))
            .Join(popup.TweenModulate(Colors.White, _tweenDuration))
            .Join(darken.TweenModulate(Colors.White, _tweenDuration))
            .Build();
        tweenBuilder.SetEasing(Easing.InOutExpo);
        tweenBuilder.Play();
        TreeExited += tweenBuilder.Kill;
    }

    public override void _Ready()
    {
        base._Ready();
        TweenPopup();
        _installLabel.SetText($"Select install directory for {GameEntry.Name} ({GameEntry.Id})");
        if (IsLocatingProcess)
        {
            _installLabel.SetText($"Find {GameEntry.ExeName} executable file");
        }

        _pathEdit.SetText(YukariConfig.Instance.ConfigData.InstallPath + "/" + GameEntry.Id);

        _pathEdit.TextChanged += PathEditOnTextChanged;
    }

    private void PathEditOnTextChanged(string newText)
    {
        GD.Print(newText);
        if (newText.IsNullOrEmpty())
        {
            _confirmButton.SetDisabled(true);
            GD.Print("empty");
            return;
        }

        if (IsLocatingProcess)
        {
            if (File.Exists(newText) && newText.EndsWith(GameEntry.ExeName))
            {
                _confirmButton.SetDisabled(false);
            }
            else
            {
                _confirmButton.SetDisabled(true);
            }

            return;
        }

        if (!IsLocatingProcess)
        {
            _confirmButton.SetDisabled(false);
            GD.Print("test");
            return;
        }

        _confirmButton.SetDisabled(false);
    }

    private void OnSelectPathPressed()
    {
        _fileDialog = IsLocatingProcess ? ConfigureLocateExeDialog() : ConfigureInstallDialog();
        AddChild(_fileDialog);
        _fileDialog.SetCurrentPath(YukariConfig.Instance.ConfigData.InstallPath);
        _fileDialog.PopupCentered();
    }

    private FileDialog ConfigureLocateExeDialog()
    {
        var fileDialog = new FileDialog();
        fileDialog.SetAccess(FileDialog.AccessEnum.Filesystem);
        fileDialog.SetFileMode(FileDialog.FileModeEnum.OpenFile);
        fileDialog.AddFilter(GameEntry.ExeName);
        fileDialog.SetShowHiddenFiles(true);
        fileDialog.SetUseNativeDialog(true);
        fileDialog.FileSelected += FileDialogOnFileSelected;
        fileDialog.SetCurrentPath(YukariConfig.Instance.ConfigData.InstallPath);
        return fileDialog;
    }


    private FileDialog ConfigureInstallDialog()
    {
        var fileDialog = new FileDialog();
        fileDialog.SetAccess(FileDialog.AccessEnum.Filesystem);
        fileDialog.SetFileMode(FileDialog.FileModeEnum.OpenDir);
        fileDialog.SetShowHiddenFiles(true);
        fileDialog.SetUseNativeDialog(true);
        fileDialog.DirSelected += FileDialogOnDirSelected;
        fileDialog.SetCurrentPath(YukariConfig.Instance.ConfigData.InstallPath);
        return fileDialog;
    }

    private void FileDialogOnDirSelected(string dir)
    {
        _pathEdit.SetText(dir);
    }

    private void FileDialogOnFileSelected(string path)
    {
        _pathEdit.SetText(path);
        _pathEdit.SetCaretColumn(int.MaxValue);
    }

    private void OnCancelPressed()
    {
        QueueFree();
    }

    private void OnConfirmPressed()
    {
        PromptConfirmed?.Invoke(_pathEdit.GetText());
        QueueFree();
    }
}