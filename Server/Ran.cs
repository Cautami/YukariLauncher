using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Downloader;
using Godot;
using YukariLauncher;
using YukariLauncher.Config;
using FileAccess = System.IO.FileAccess;
using HttpClient = System.Net.Http.HttpClient;

public partial class Ran : Node
{
    public static Ran Instance { get; private set; }

    private StringName _apiAddress;
    public event Action<string> DownloadRequested;
    public event Action<string, float> DownloadProgress;
    public event Action<string> DownloadCompleted;
    public event Action<string> DownloadFailed;
    public event Action<string, string> InstallComplete;
    public event Action GameListRetrieved;

    public event Action ConfigSyncStart;

    private List<string> _downloadableGames = [];

    private HttpClient _httpClient;

    public override async void _EnterTree()
    {
        base._EnterTree();
        Instance    = this;
        _httpClient = new HttpClient();
        _apiAddress = YukariConfig.GetApiAddress();
        await GetDownloadableGames();
        GameListRetrieved?.Invoke();
    }

    private async Task GetDownloadableGames()
    {
        var response = await MakeRequest($"{_apiAddress}/games/exists/all");
        if (response.IsSuccessStatusCode)
        {
            _downloadableGames = await response.Content.ReadFromJsonAsync(ServerContext.Default.ListString);
            YukariConfig.Instance.ConfigData.DownloadableGamesCache.Clear();
            YukariConfig.Instance.ConfigData.DownloadableGamesCache = _downloadableGames;
        }
        else
        {
            GD.PrintErr("Couldnt retrieve list of downloadable games");
        }
    }

    public void DownloadGame(GameEntryResource gameEntry)
    {
        if (!YukariConfig.IsGameDownloadable(gameEntry.Id))
        {
            return;
        }

        var installPopup = GD.Load<PackedScene>("uid://q43qt7r1w810").Instantiate<InstallPopup>();
        installPopup.GameEntry = gameEntry;
        GetViewport().AddChild(installPopup);
        installPopup.PromptConfirmed += path => _ = InstallPopupOnPromptConfirmed(path, gameEntry);
    }

    private async Task InstallPopupOnPromptConfirmed(string path, GameEntryResource gameEntry)
    {
        Callable.From(() => DownloadRequested?.Invoke(gameEntry.Id)).CallDeferred();
        var downloadCachePath = YukariConfig.Instance.ConfigData.DownloadPath;
        Directory.CreateDirectory(downloadCachePath);
        Directory.CreateDirectory(path);
        path = path.TrimEnd(new char['/']);
        if (!File.Exists($"{path}/{gameEntry.ProcessName}"))
        {
            if (gameEntry.Chronology == GameChronology.PC98)
            {
                if (!File.Exists(downloadCachePath + "/dosbox-x.zip") &&
                    !File.Exists(YukariConfig.Instance.ConfigData.DosboxPath + "/dosbox-x.exe"))
                {
                    var downloadLinkResponse = await MakeRequest($"{_apiAddress}/games/download/dosbox-x");
                    if (!downloadLinkResponse.IsSuccessStatusCode)
                    {
                        DownloadFailed?.Invoke(gameEntry.Id);
                        return;
                    }

                    var downloadLink =
                        await downloadLinkResponse.Content.ReadFromJsonAsync(ServerContext.Default.DownloadResponse);

                    GD.Print(downloadLink.Url.ToString());

                    var downloader = new DownloadService(new DownloadConfiguration
                    {
                        ChunkCount       = 8,
                        ParallelDownload = true,
                    });

                    downloader.DownloadProgressChanged += (_, e) =>
                        Callable.From(() => DownloadProgress?.Invoke(gameEntry.Id, (float)e.ProgressPercentage))
                            .CallDeferred();
                    await downloader.DownloadFileTaskAsync(downloadLink.Url, $"{downloadCachePath}/dosbox.zip");
                    await ZipFile.ExtractToDirectoryAsync($"{downloadCachePath}/dosbox.zip",
                        YukariConfig.Instance.ConfigData.DosboxPath);
                }
            }

            if (!File.Exists($"{downloadCachePath}/{gameEntry.Id}.zip"))
            {
                var downloadLinkResponse = await MakeRequest($"{_apiAddress}/games/download/{gameEntry.Id}");
                if (!downloadLinkResponse.IsSuccessStatusCode)
                {
                    DownloadFailed?.Invoke(gameEntry.Id);
                    return;
                }

                var downloadLink =
                    await downloadLinkResponse.Content.ReadFromJsonAsync(ServerContext.Default.DownloadResponse);

                var downloader = new DownloadService(new DownloadConfiguration
                {
                    ChunkCount       = 8,
                    ParallelDownload = true,
                });

                downloader.DownloadProgressChanged += (_, e) =>
                    Callable.From(() => DownloadProgress?.Invoke(gameEntry.Id, (float)e.ProgressPercentage))
                        .CallDeferred();

                await downloader.DownloadFileTaskAsync(downloadLink.Url, $"{downloadCachePath}/{gameEntry.Id}.zip");

                GD.Print($"Done installing {gameEntry.Id} :D");
                Callable.From(() => DownloadCompleted?.Invoke(gameEntry.Id)).CallDeferred();
            }

            await ZipFile.ExtractToDirectoryAsync($"{downloadCachePath}/{gameEntry.Id}.zip", path);
            GD.Print("Unzipped and ready to play");
            File.Delete($"{downloadCachePath}/{gameEntry.Id}.zip");
        }

        Callable.From(() => InstallComplete?.Invoke(gameEntry.Id, path)).CallDeferred();
    }

    private void HandlePC98() { }

    private async Task<HttpResponseMessage> MakeRequest(string url)
    {
        var token = await GetToken();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            token = await RefreshToken();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            response = await _httpClient.GetAsync(url);
        }

        return response;
    }

    private async Task<string> GetToken()
    {
        var token = OS.GetEnvironment("JwToken");

        if (token.IsNullOrEmpty() || IsTokenExpiringSoon(token, 2))
        {
            token = await RefreshToken();
        }

        return token;
    }

    private bool IsTokenExpiringSoon(string token, int thresholdMinutes)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.ValidTo <= DateTime.UtcNow.AddMinutes(thresholdMinutes);
    }

    private async Task<string> RefreshToken()
    {
        var body = new StringContent(JsonSerializer.Serialize(
            new TokenRequest { Secret = ProjectSettings.GetSetting("application/config/yukari_secret").AsString() },
            ServerContext.Default.TokenRequest), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_apiAddress}/auth/token", body);
        if (!response.IsSuccessStatusCode)
        {
            GD.PrintErr("Auth Token returned no good");
            return null;
        }

        var newToken = await response.Content.ReadFromJsonAsync(ServerContext.Default.TokenResponse);
        OS.SetEnvironment("JwToken", newToken.Token);
        return newToken.Token;
    }


    [JsonSerializable(typeof(TokenRequest)), JsonSerializable(typeof(TokenResponse)),
     JsonSerializable(typeof(List<string>)), JsonSerializable(typeof(DownloadResponse))]
    internal partial class ServerContext : JsonSerializerContext { }

    public class TokenRequest
    {
        public string Secret { get; set; }
    }

    public class TokenResponse
    {
        [JsonPropertyName("token")] public string Token { get; set; }
    }

    public class DownloadResponse
    {
        [JsonPropertyName("url")] public string Url { get; set; }
    }
}