using Microsoft.EntityFrameworkCore;

namespace YouTubePlaylistWatcher_desktop.Models;

public interface IAppDbContext
{
    DbSet<T> Set<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    public bool IsInTransaction();
}

public partial class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Playlist> Playlists { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Playlist>().HasKey(p => p.Id);
        builder.Entity<Playlist>().Property(p => p.Id).HasColumnName("id");
        builder.Entity<Playlist>().Property(p => p.ETag).HasColumnName("etag");
        builder.Entity<Playlist>().Property(p => p.Title).HasColumnName("title");
        builder.Entity<Playlist>().Property(p => p.Description).HasColumnName("description");
        builder.Entity<Playlist>().Property(p => p.ChannelId).HasColumnName("channel_id");
        builder.Entity<Playlist>().HasIndex(p => p.Id).IsUnique();
        builder.Entity<Playlist>().ToTable("playlists");

        builder.Entity<Video>().HasKey(v => new
        {
            v.Id,
            v.PlaylistId
        });
        builder.Entity<Video>().Property(v => v.Id).HasColumnName("id");
        builder.Entity<Video>().Property(v => v.ETag).HasColumnName("etag");
        builder.Entity<Video>().Property(v => v.PlaylistId).HasColumnName("playlist_id");
        builder.Entity<Video>().Property(v => v.Title).HasColumnName("title");
        builder.Entity<Video>().Property(v => v.Description).HasColumnName("description");
        builder.Entity<Video>().Property(v => v.ChannelId).HasColumnName("channel_id");
        builder.Entity<Video>().Property(v => v.ChannelTitle).HasColumnName("channel_title");
        builder.Entity<Video>().ToTable("videos");

        builder.Entity<Playlist>()
            .HasMany(p => p.Videos)
            .WithOne(v => v.Playlist)
            .HasForeignKey(v => v.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public async Task BeginTransactionAsync()
    {
        await Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        await Database.RollbackTransactionAsync();
    }

    public bool IsInTransaction()
    {
        return Database.CurrentTransaction != null;
    }
}