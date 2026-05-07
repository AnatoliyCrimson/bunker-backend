using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BunkerGame.Models;
using BunkerGame.Models.GameModels;

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
    public DbSet<AiPrompt> AiPrompts => Set<AiPrompt>();
    public DbSet<GameEventLog> GameEventLogs => Set<GameEventLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Настройка User
        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.Id).ValueGeneratedNever();
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();

            // 1. Связь User -> Room (Многие к Одному)
            // Пользователь может быть только в одной комнате.
            entity.HasOne(u => u.CurrentRoom)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.CurrentRoomId)
                .OnDelete(DeleteBehavior.SetNull); // Если комната удалена, пользователь просто "выходит" из нее

            // 2. Связь User -> Game (Многие к Одному)
            // Пользователь может быть только в одной игре.
             entity.HasOne(u => u.CurrentGame)
                 .WithMany() // В Game нет списка Users (там список Players), поэтому пусто
                 .HasForeignKey(u => u.CurrentGameId)
                 .OnDelete(DeleteBehavior.SetNull); // Если игра удалена, пользователь "выходит" из игры
             
             // 3. Связь User -> Player (Один к Одному)
             // У пользователя есть ровно один текущий персонаж (если он в игре).
             entity.HasOne(u => u.CurrentPlayerCharacter)
                 .WithOne(p => p.User)
                 .HasForeignKey<Player>(p => p.UserId)// Внешний ключ находится в таблице Players
                 .OnDelete(DeleteBehavior.Cascade); // Если удалили юзера, удаляем и его персонажа
        });

        // Настройка Room
        builder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.HasIndex(r => r.InviteCode).IsUnique();
        });

        // Настройка Game
        builder.Entity<Game>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Id).ValueGeneratedNever();
            
            // 4. Связь Game -> Room (Один к Одному)
            // Игра привязана к одной комнате.
            entity.HasOne(g => g.Room)
                .WithOne(r => r.Game)
                .HasForeignKey<Game>(g => g.RoomId)
                .OnDelete(DeleteBehavior.Cascade); // ВАЖНО: Если удаляем комнату, каскадно удаляется игра!
            
            entity.Property(g => g.CurrentVotes).HasColumnType("jsonb");
            entity.Property(g => g.BunkerRooms).HasColumnType("jsonb");
        });
        
        // Настройка AiPrompt
        builder.Entity<AiPrompt>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Code).IsUnique();
        });

        // Настройка GameEventLog
        builder.Entity<GameEventLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne<Game>()
                .WithMany()
                .HasForeignKey(e => e.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Настройка Player
        builder.Entity<Player>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedNever();
            
            // 5. Связь Player -> Game (Многие к Одному)
            entity.HasOne(p => p.Game)
                .WithMany(g => g.Players)
                .HasForeignKey(p => p.GameId)
                .OnDelete(DeleteBehavior.Cascade); // ВАЖНО: Если удаляем игру, каскадно удаляются все персонажи!
                
            // Хранение характеристик в формате JSONB
            entity.Property(p => p.Characteristics).HasColumnType("jsonb");
            entity.Property(p => p.PresentationTraitKeys).HasColumnType("jsonb");
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
    }
}