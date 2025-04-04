using Google.Apis.YouTube.v3.Data;
using Microsoft.EntityFrameworkCore;
using YouTubePlaylistWatcher_desktop.Models;
using Playlist = YouTubePlaylistWatcher_desktop.Models.Playlist;
using Video = YouTubePlaylistWatcher_desktop.Models.Video;

namespace YouTubePlaylistWatcher_desktop.Services;

public interface IVideoService
{
    Task FetchAndSaveVideosAsync();
}

public class VideoService : IVideoService
{
    private readonly IYouTubeServiceWrapper _youTubeServiceWrapper;
    private readonly IAppDbContext _db;

    public VideoService(IYouTubeServiceWrapper youTubeServiceWrapper, IAppDbContext db)
    {
        _youTubeServiceWrapper = youTubeServiceWrapper;
        _db = db;
    }

    public async Task FetchAndSaveVideosAsync()
    {
        var currentPlaylists = await _db.Set<Playlist>().ToListAsync(); //TODO exclude removed playlists

        foreach (var playlist in currentPlaylists)
        {
            var videos = await _youTubeServiceWrapper.ListVideosAsync(playlist.Id);
            var currentVideos = await _db.Set<Video>().ToListAsync();

            foreach (var video in videos)
            {
                var existingVideo = currentVideos.Find(x => x.Id == video.Id);
                if (existingVideo != null && existingVideo.ETag != video.ETag)
                {
                    await UpdateVideoAsync(existingVideo, video);
                }
                else if (existingVideo == null)
                {
                    await AddVideoAsync(video);
                }
            }

            /*foreach (var existingVideo in currentVideos.Where(existingVideo =>
                         videos.All(x => x.Id != existingVideo.Id)))
            {
                //TODO notification about removed video - add flag to db to skip in next run
                Console.WriteLine($"Removed video: {existingVideo.Title}");
            }*/
        }
    }

    private async Task AddVideoAsync(PlaylistItem video)
    {
        var newVideo = new Video
        {
            Id = video.Id,
            ETag = video.ETag,
            PlaylistId = video.Snippet.PlaylistId,
            Title = video.Snippet.Title,
            Description = video.Snippet.Description,
            ChannelId = video.Snippet.ChannelId,
            ChannelTitle = video.Snippet.VideoOwnerChannelTitle ?? "___MISSING VALUE FROM YT___"
        };

        await _db.Set<Video>().AddAsync(newVideo);
        await _db.SaveChangesAsync();
        Console.WriteLine($"Added video: {newVideo.Title} (ID: {newVideo.Id})");
    }

    private async Task UpdateVideoAsync(Video existingVideo, PlaylistItem video)
    {
        existingVideo.ETag = video.ETag;
        existingVideo.PlaylistId = video.Snippet.PlaylistId;
        existingVideo.Title = video.Snippet.Title;
        existingVideo.Description = video.Snippet.Description;
        existingVideo.ChannelId = video.Snippet.ChannelId;
        existingVideo.ChannelTitle = video.Snippet.VideoOwnerChannelTitle;

        _db.Set<Video>().Update(existingVideo);
        await _db.SaveChangesAsync();
        Console.WriteLine($"Updated video: {existingVideo.Title} (ID: {existingVideo.Id})");
    }
}