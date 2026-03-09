using System;
using Godot;
using YukariLauncher;
using YukariLauncher.Config;
using HttpClient = System.Net.Http.HttpClient;

public partial class Ran : Node
{
    public static Ran Instance { get; private set; }

    //TODO: do server stuff
    public const string DefaultApiAddress = "";
    private StringName ApiAddress;
    public event Action<GameEntryResource> GameRequested;
    public event Action<GameEntryResource> GameInstalled;

    public event Action ConfigSyncStart;

    private HttpClient _httpRequest;

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance     = this;
        _httpRequest = new HttpClient();
    }

    public override void _Ready()
    {
        base._Ready();
        ApiAddress = YukariConfig.GetApiAddress();
    }

    public void DownloadGame(GameEntryResource gameEntry)
    {
        if (!IsGameDownloadable(gameEntry.Id))
        {
            return;
        }

        var installPopup = GD.Load<PackedScene>("uid://q43qt7r1w810").Instantiate<InstallPopup>();
        GetViewport().AddChild(installPopup);
    }

    public static bool IsGameDownloadable(string id)
    {
        return true;
    }
}