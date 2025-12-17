using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BunkerGame.Models;

namespace BunkerGame.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Player> Players => Set<Player>(); // Добавим DbSet для игроков
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Настройка User
        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.Id).ValueGeneratedNever();
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();

            // Связь User -> Room (Многие к Одному)
            entity.HasOne(u => u.CurrentRoom)
                .WithMany(r => r.Players)
                .HasForeignKey(u => u.CurrentRoomId)
                .OnDelete(DeleteBehavior.SetNull); // ВАЖНО: Если комната удалена, пользователь просто становится "свободным"

             // Связь User -> Game (Опционально, если нужно поле CurrentGameId)
             entity.HasOne(u => u.CurrentGame)
                 .WithMany() // В Game пока нет списка Users, можно оставить пустым
                 .HasForeignKey(u => u.CurrentGameId)
                 .OnDelete(DeleteBehavior.SetNull);
             
             // Связь User -> Player (1 к 0..1)
             // Пользователь имеет одного текущего персонажа
             entity.HasOne(u => u.CurrentPlayerCharacter)
                 .WithOne(p => p.User)
                 .HasForeignKey<Player>(p => p.UserId) // Player зависит от User
                 .OnDelete(DeleteBehavior.Cascade); // Если удалили юзера, удаляем и его персонажа
        });

        // Настройка Room
        builder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.HasIndex(r => r.InviteCode).IsUnique(); // Код приглашения уникален
        });

        // Настройка Game
        builder.Entity<Game>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Id).ValueGeneratedNever();
        });

        // Настройка RefreshToken (без изменений)
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Token).IsRequired().HasMaxLength(256);
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Явно говорим EF, что Characteristics нужно хранить как JSONB
        builder.Entity<Player>()
            .Property(p => p.Characteristics)
            .HasColumnType("jsonb");
            
        // То же самое для старого списка ключей (если еще не было настроено)
        builder.Entity<Player>()
            .Property(p => p.RevealedTraitKeys)
            .HasColumnType("jsonb");
    }
}