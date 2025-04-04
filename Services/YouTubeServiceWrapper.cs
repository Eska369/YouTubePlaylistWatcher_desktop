using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;

namespace YouTubePlaylistWatcher_desktop.Services;

public interface IYouTubeServiceWrapper
{
    Task<string> GetCurrentChannelAsync();
    Task<PlaylistsResource.ListRequest> GetPlaylistAsync(string playlistId);
    Task<List<Playlist>> ListPlaylistsAsync();
}

public class YouTubeServiceWrapper : IYouTubeServiceWrapper
{
    private readonly ILogger<YouTubeServiceWrapper> _logger;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly string[] _parts = ["snippet", "contentDetails", "localizations", "player", "status"];
    private readonly string _partsString;

    public YouTubeServiceWrapper(ILogger<YouTubeServiceWrapper> logger, IGoogleAuthService googleAuthService)
    {
        _logger = logger;
        _googleAuthService = googleAuthService;
        _partsString = string.Join(",", _parts);
    }

    private async Task<YouTubeService> InitializeYouTubeService()
    {
        return new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = await _googleAuthService.Authenticate(),
            ApplicationName = "YouTube Playlist Fetcher"
        });
    }

    public async Task<string> GetCurrentChannelAsync()
    {
        var youtubeService = await InitializeYouTubeService();

        var channelsRequest = youtubeService.Channels.List("snippet");
        channelsRequest.Mine = true;

        var channelsResponse = await channelsRequest.ExecuteAsync();
        _logger.LogInformation("Current channel info:");
        _logger.LogInformation("- {SnippetTitle} (ID: {ChannelId})", channelsResponse.Items[0].Snippet.Title,
            channelsResponse.Items[0].Id);
        return channelsResponse.Items[0].Id;
    }

    public async Task<PlaylistsResource.ListRequest> GetPlaylistAsync(string playlistId)
    {
        var youtubeService = await InitializeYouTubeService();

        var playlistsRequest = youtubeService.Playlists.List(_partsString);
        playlistsRequest.MaxResults = 50;
        playlistsRequest.Id = playlistId;
        var playlistsResponse = await playlistsRequest.ExecuteAsync();
        _logger.LogInformation("Playlist info:");
        _logger.LogInformation("- {SnippetTitle} (ID: {PlaylistId})", playlistsResponse.Items[0].Snippet.Title,
            playlistsResponse.Items[0].Id);

        return playlistsRequest;
    }

    public async Task<List<Playlist>> ListPlaylistsAsync()
    {
        var youtubeService = await InitializeYouTubeService();

        var result = new List<Playlist>();

        var playlistsRequest = youtubeService.Playlists.List(_partsString);
        playlistsRequest.MaxResults = 50;
        playlistsRequest.PageToken = null;
        string? nextPageToken = null;
        playlistsRequest.ChannelId = MemoryStorage.UserId;

        do
        {
            if (nextPageToken != null)
            {
                playlistsRequest.PageToken = nextPageToken;
            }

            var playlistsResponse = await playlistsRequest.ExecuteAsync();
            result.AddRange(playlistsResponse.Items);
            nextPageToken = playlistsResponse.NextPageToken;
        } while (nextPageToken != null);

        _logger.LogInformation("Your playlists:");
        foreach (var playlist in result)
        {
            Console.WriteLine(playlist.Snippet.Title + " (ID: " + playlist.Id + ")");
        }

        return result;
    }
}