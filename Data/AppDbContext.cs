using KanbanBoardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoardAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<KanbanTask> KanbanTasks => Set<KanbanTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Конфигурация Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles"); // Явное указание имени таблицы
            entity.HasKey(r => r.RoleId);
            entity.Property(r => r.RoleName).HasMaxLength(50);
        });

        // Конфигурация User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.Username)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            // Связь с Role
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Конфигурация KanbanTask
        modelBuilder.Entity<KanbanTask>(entity =>
        {
            entity.HasKey(t => t.TaskId);

            entity.Property(t => t.Title)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(t => t.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ToDo");

            // Связи с User
            entity.HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedKanbanTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.AssignedByUser)
                .WithMany(u => u.CreatedKanbanTasks)
                .HasForeignKey(t => t.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Инициализация начальных данных
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "User" },
            new Role { RoleId = 3, RoleName = "Manager" }
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Глобальные настройки для всех строковых свойств
        configurationBuilder.Properties<string>()
            .HaveMaxLength(200); // Дефолтная максимальная длина

        // Настройка точности для DateTime
        configurationBuilder.Properties<DateTime>()
            .HavePrecision(3); // Точность до миллисекунд
    }
}