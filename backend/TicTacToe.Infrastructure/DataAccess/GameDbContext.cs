using Microsoft.EntityFrameworkCore;
namespace TicTacToe.Infrastructure;

public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<GameEntity> Games => Set<GameEntity>();
    public DbSet<ScoreboardEntity> Scoreboards => Set<ScoreboardEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameEntity>(entity =>
        {
            entity.HasKey(game => game.Id);
            entity.Property(game => game.StateJson).IsRequired();
            entity.Property(game => game.RowVersion).IsRowVersion();
            entity.HasIndex(game => game.UpdatedAt);
        });
        modelBuilder.Entity<ScoreboardEntity>().HasKey(scoreboard => scoreboard.Id);
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(user => user.Username);
            entity.Property(user => user.Username).HasMaxLength(100);
            entity.Property(user => user.Role).HasMaxLength(32);
            entity.Property(user => user.PasswordHash).IsRequired();
        });
    }
}
