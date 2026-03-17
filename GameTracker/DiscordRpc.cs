using System.Collections.Generic;
using System.Linq;
using DiscordRPC;
using DiscordRPC.Message;
using Godot;
using YukariLauncher;
using YukariLauncher.Config;

namespace YukariApp.GameTracker;

public partial class DiscordRpc : Node
{
    public static DiscordRpc Instance { get; private set; }
    private DiscordRpcClient _client;
    private const string APP_ID = "1475771294716002518";

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
    }

    public override void _Ready()
    {
        base._Ready();

        if (YukariConfig.Instance.ConfigData.DiscordRpcEnabled)
        {
            SetPresence(null);
        }

        Chen.Instance.GameDetected += InstanceOnGameDetected;
        Chen.Instance.GameClosed   += InstanceOnGameClosed;
    }

    private void InstanceOnGameClosed(GameEntryResource obj)
    {
        SetPresence(null);
    }

    private void InstanceOnGameDetected(GameEntryResource gameEntry)
    {
        SetPresence(gameEntry);
    }

    private void SetPresence(GameEntryResource gameEntry)
    {
        _client?.Dispose();
        var appId = "";
        if (gameEntry is not null)
        {
            appId = GetDiscordAppId(gameEntry.Id);
        }

        if (appId.IsNullOrEmpty())
        {
            appId = APP_ID;
        }

        _client = new DiscordRpcClient(appId);

        _client.OnReady += ClientOnOnReady;
        _client.OnError += (sender, e) =>
            GD.Print($"Discord error {e.Message}");
        _client.Initialize();
        _client.SetPresence(new RichPresence
        {
            Details    = "WIP Detail",
            State      = "WIP State",
            Timestamps = Timestamps.Now,
            Assets = new Assets
            {
                LargeImageKey  = "yukarilauncher",
                LargeImageText = "Yukari Test",
            },
        });
    }

    public void TogglePresence(bool toggledOn)
    {
        if (toggledOn)
        {
            SetPresence(null);
        }
        else
        {
            _client?.Dispose();
        }
    }

    private void ClientOnOnReady(object sender, ReadyMessage args) { }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        _client?.Invoke();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _client?.Dispose();
    }

    private string GetDiscordAppId(string id)
    {
        return GameAppIdMap[id];
    }

    //Unfortunately, you cannot seemingly change the app name, therefore reinit of a rpc client is necessary
    //Yucky? I agree!
    private Dictionary<string, string> GameAppIdMap = new()
    {
        { "th01", "1481193340488781914" },
        { "th02", "1481193444859838485" },
        { "th03", "1481193544021573632" },
        { "th04", "1481193623340060724" },
        { "th05", "1481193724603142167" },
        { "th06", "1481193029166694432" },
        { "th07", "1481193793775468581" },
        { "th07.5", "1481193944212570275" },
        { "th08", "1481194166242250754" },
        { "th09", "1481204221092167740" },
        { "th09.5", "1481204289123909779" },
        { "th10", "1481214023767949423" },
        { "th10.5", "1481214097503551601" },
        { "th11", "1481528166278500374" },
        { "th12", "1481528376702533633" },
        { "th12.3", "1481528700293091458" },
        { "th12.5", "1481528762553336000" },
        { "th12.8", "1481529745765175417" },
        { "th13", "1481531829441007649" },
        { "th13.5", "1481531942892736542" },
        { "th14", "1481532081342251068" },
        { "th14.3", "1481532348624408656" },
        { "th14.5", "1481533159538692180" },
        { "th15", "1481533233148723343" },
        { "th15.5", "1481724301877969148" },
        { "th16", "1481724396082299002" },
        { "th16.5", "1481724460619792384" },
        { "th17", "1481729024865603795" },
        { "th17.5", "1481729100543692942" },
        { "th18", "1481729202142052553" },
        { "th18.5", "1481733153696776274" },
        { "th19", "1481733811782815774" }, //damn name too big
        { "th20", "1481733957799252061" },
    };
}