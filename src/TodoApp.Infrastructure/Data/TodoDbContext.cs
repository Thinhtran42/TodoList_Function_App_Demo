using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data;

public class TodoDbContext : BaseDbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");
            entity.ToTable("users", "app");

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("email");

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("password_hash");

            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");

            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");

            entity.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at");

            // Unique constraints
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("idx_users_username_unique");
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("idx_users_email_unique");

            // One-to-many relationship with TodoItems
            entity.HasMany(e => e.TodoItems)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");
            entity.ToTable("refresh_tokens", "app");

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("token");

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasColumnName("user_id");

            entity.Property(e => e.ExpiresAt)
                .IsRequired()
                .HasColumnName("expires_at");

            entity.Property(e => e.IsRevoked)
                .HasDefaultValue(false)
                .HasColumnName("is_revoked");

            // Foreign key relationship
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for performance
            entity.HasIndex(e => e.Token).IsUnique().HasDatabaseName("idx_refresh_tokens_token_unique");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_refresh_tokens_user_id");
        });

        // Configure TodoItem entity
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("todo_items_pkey");

            entity.ToTable("todo_items", "app");

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("title");

            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .HasColumnName("description");

            entity.Property(e => e.IsCompleted)
                .HasDefaultValue(false)
                .HasColumnName("is_completed");

            entity.Property(e => e.Priority)
                .HasConversion<int>()
                .HasDefaultValue(Priority.Medium)
                .HasColumnName("priority");

            entity.Property(e => e.Category)
                .HasConversion<int>()
                .HasDefaultValue(Category.General)
                .HasColumnName("category");

            entity.Property(e => e.DueDate)
                .HasColumnName("due_date");

            entity.Property(e => e.Tags)
                .HasMaxLength(500)
                .HasColumnName("tags");

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasColumnName("user_id");

            // Foreign key relationship
            entity.HasOne(e => e.User)
                .WithMany(e => e.TodoItems)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for performance
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_todo_items_user_id");
        });

        base.OnModelCreating(modelBuilder);
    }
}