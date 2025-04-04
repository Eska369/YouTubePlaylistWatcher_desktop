namespace YouTubePlaylistWatcher_desktop.Models;

public class Video
{
    public string Id { get; set; }
    public string ETag { get; set; }
    public string PlaylistId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ChannelId { get; set; }
    public string ChannelTitle { get; set; }

    public virtual Playlist Playlist { get; set; }
}