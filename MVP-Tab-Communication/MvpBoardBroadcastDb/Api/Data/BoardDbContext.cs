using Microsoft.EntityFrameworkCore;
using MvpBoardBroadcastDb.Api.Models;

namespace MvpBoardBroadcastDb.Api.Data;

public sealed class BoardDbContext(DbContextOptions<BoardDbContext> options) : DbContext(options)
{
    public DbSet<BoardItemEntity> BoardItems => Set<BoardItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BoardItemEntity>(entity =>
        {
            entity.ToTable("board_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.Title).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Color).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ZoneId).HasMaxLength(64).IsRequired();
        });
    }
}
