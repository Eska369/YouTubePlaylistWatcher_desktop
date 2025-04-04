namespace YouTubePlaylistWatcher_desktop.Models;

public class Playlist
{
    public string Id { get; set; }
    public string ETag { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ChannelId { get; set; }

    public ICollection<Video> Videos { get; set; } = [];
}