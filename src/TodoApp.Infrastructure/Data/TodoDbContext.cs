using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data;

public class TodoDbContext : BaseDbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
        });

        base.OnModelCreating(modelBuilder);
    }
}