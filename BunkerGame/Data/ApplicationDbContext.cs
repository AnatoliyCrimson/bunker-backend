using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BunkerGame.Models;
using Microsoft.AspNetCore.Identity;

namespace BunkerGame.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Game> Games => Set<Game>();
    // --- НОВОЕ: Добавляем DbSet для RefreshToken ---
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    // --- КОНЕЦ НОВОГО ---

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.Id).ValueGeneratedNever();
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();
        });

        builder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
        });

        builder.Entity<Game>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Id).ValueGeneratedNever();
        });

        // --- НОВОЕ: Настройка RefreshToken ---
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);

            entity.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(256); // Длина, соответствующая модели

            entity.HasIndex(rt => rt.Token).IsUnique(); // Токен должен быть уникальным

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens) // Предполагаем, что в User будет коллекция RefreshTokens
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Удалять токены при удалении пользователя
        });
        // --- КОНЕЦ НОВОГО ---
    }
}