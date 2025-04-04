using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YouTubePlaylistWatcher_desktop.Models;

namespace YouTubePlaylistWatcher_desktop.Services;

public interface IPlaylistService
{
    Task FetchAndSavePlaylistsAsync();
}

public class PlaylistService : IPlaylistService
{
    private readonly ILogger<PlaylistService> _logger;
    private readonly IYouTubeServiceWrapper _youtubeService;
    private readonly IAppDbContext _db;

    public PlaylistService(ILogger<PlaylistService> logger, IYouTubeServiceWrapper youtubeService, IAppDbContext db)
    {
        _logger = logger;
        _youtubeService = youtubeService;
        _db = db;
    }

    public async Task FetchAndSavePlaylistsAsync()
    {
        var playlists = await _youtubeService.ListPlaylistsAsync();
        var currentPlaylists = await _db.Set<Playlist>().ToListAsync();

        foreach (var playlist in playlists)
        {
            var existingPlaylist = currentPlaylists.Find(x => x.Id == playlist.Id);
            if (existingPlaylist != null && existingPlaylist.ETag != playlist.ETag)
            {
                await UpdatePlaylistAsync(existingPlaylist, playlist);
            }
            else if (existingPlaylist == null)
            {
                await AddPlaylistAsync(playlist);
            }
        }

        foreach (var existingPlaylist in currentPlaylists.Where(existingPlaylist => playlists.All(x => x.Id != existingPlaylist.Id)))
        {
            //TODO notification about removed playlist - add flag to db to skip in next run
            _logger.LogInformation("Removed playlist: {Title}", existingPlaylist.Title);
        }

        await _db.SaveChangesAsync();
    }

    private async Task UpdatePlaylistAsync(Playlist existingPlaylist, Google.Apis.YouTube.v3.Data.Playlist playlist)
    {
        existingPlaylist.ETag = playlist.ETag;
        existingPlaylist.Title = playlist.Snippet.Title;
        existingPlaylist.Description = playlist.Snippet.Description;
        existingPlaylist.ChannelId = playlist.Snippet.ChannelId;

        _db.Set<Playlist>().Update(existingPlaylist);
        await _db.SaveChangesAsync();

        if (existingPlaylist.Title == "Favorites")
            return; // Favorites playlist every time returns new ETag for some reason

        _logger.LogInformation("Updated playlist: {Title}", existingPlaylist.Title);
    }

    private async Task AddPlaylistAsync(Google.Apis.YouTube.v3.Data.Playlist playlist)
    {
        var newEntry = new Playlist()
        {
            Id = playlist.Id,
            ETag = playlist.ETag,
            Title = playlist.Snippet.Title,
            Description = playlist.Snippet.Description,
            ChannelId = playlist.Snippet.ChannelId,
        };

        await _db.Set<Playlist>().AddAsync(newEntry);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Added new playlist: {Title}", newEntry.Title);
    }
}