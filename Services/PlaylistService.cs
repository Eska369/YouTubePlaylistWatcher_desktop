using Google.Apis.YouTube.v3.Data;
using Microsoft.EntityFrameworkCore;
using YouTubePlaylistWatcher_desktop.Models;
using Playlist = YouTubePlaylistWatcher_desktop.Models.Playlist;

namespace YouTubePlaylistWatcher_desktop.Services;

public interface IPlaylistService
{
    Task FetchAndSavePlaylistsAsync();
}

public class PlaylistService : IPlaylistService
{
    private readonly IYouTubeServiceWrapper _youtubeService;
    private readonly IAppDbContext _db;

    public PlaylistService(IYouTubeServiceWrapper youtubeService, IAppDbContext db)
    {
        _youtubeService = youtubeService;
        _db = db;
    }

    public async Task FetchAndSavePlaylistsAsync()
    {
        var playlists = await _youtubeService.ListPlaylistsAsync();
        // Positions below are not included in standard - needed to add them manualy
        playlists.Add(new Google.Apis.YouTube.v3.Data.Playlist(){Id = "LL",Snippet = new PlaylistSnippet(){Title = "Liked Videos"}});
        playlists.Add(new Google.Apis.YouTube.v3.Data.Playlist(){Id = "WL",Snippet = new PlaylistSnippet(){Title = "Watch Later"}});

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

        foreach (var existingPlaylist in currentPlaylists.Where(existingPlaylist =>
                     playlists.All(x => x.Id != existingPlaylist.Id)))
        {
            //TODO notification about removed playlist - add flag to db to skip in next run
            Console.WriteLine($"Removed playlist: {existingPlaylist.Title}");
        }

        await _db.SaveChangesAsync();
    }

    private async Task UpdatePlaylistAsync(Playlist existingPlaylist, Google.Apis.YouTube.v3.Data.Playlist playlist)
    {
        existingPlaylist.ETag = playlist.ETag ?? "__MISSING__";
        existingPlaylist.Title = playlist.Snippet.Title ?? "__MISSING__";
        existingPlaylist.Description = playlist.Snippet.Description ?? "__MISSING__";
        existingPlaylist.ChannelId = playlist.Snippet.ChannelId ?? "__MISSING__";

        _db.Set<Playlist>().Update(existingPlaylist);
        await _db.SaveChangesAsync();

        if (existingPlaylist.Title == "Favorites")
            return; // Favorites playlist every time returns new ETag for some reason

        Console.WriteLine($"Updated playlist: {existingPlaylist.Title}");
    }

    private async Task AddPlaylistAsync(Google.Apis.YouTube.v3.Data.Playlist playlist)
    {
        var newEntry = new Playlist()
        {
            Id = playlist.Id,
            ETag = playlist.ETag ?? "__MISSING__",
            Title = playlist.Snippet.Title,
            Description = playlist.Snippet.Description ?? "__MISSING__",
            ChannelId = playlist.Snippet.ChannelId ?? "__MISSING__",
        };

        await _db.Set<Playlist>().AddAsync(newEntry);
        await _db.SaveChangesAsync();
        Console.WriteLine($"Added new playlist: {newEntry.Title}");
    }
}