using Eymta.core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Eymta.Repository.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
        public DbSet<TaskComment> TaskComments => Set<TaskComment>();
        public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<TeamMessage> TeamMessages => Set<TeamMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── User ──────────────────────────────────────────────────────────
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.Id);
                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => u.Username).IsUnique();
                e.Property(u => u.Username).HasMaxLength(50).IsRequired();
                e.Property(u => u.Email).HasMaxLength(150).IsRequired();
                e.Property(u => u.PasswordHash).IsRequired();
                e.Property(u => u.Role).HasConversion<string>();

                // User belongs to one Team
                e.HasOne(u => u.Team)
                 .WithMany(t => t.Members)
                 .HasForeignKey(u => u.TeamId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Team ──────────────────────────────────────────────────────────
            modelBuilder.Entity<Team>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Name).HasMaxLength(100).IsRequired();
                e.Property(t => t.Color).HasMaxLength(10);

                // Team has one Leader (a User) — no cascade to avoid cycles
                e.HasOne(t => t.Leader)
                 .WithMany()
                 .HasForeignKey(t => t.LeaderId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── TaskItem ──────────────────────────────────────────────────────
            modelBuilder.Entity<TaskItem>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Title).HasMaxLength(200).IsRequired();
                e.Property(t => t.Status).HasConversion<string>();
                e.Property(t => t.Priority).HasConversion<string>();

                // Assigned to User (nullable)
                e.HasOne(t => t.AssignedToUser)
                 .WithMany(u => u.AssignedTasks)
                 .HasForeignKey(t => t.AssignedToUserId)
                 .OnDelete(DeleteBehavior.SetNull);

                // Assigned to Team (nullable)
                e.HasOne(t => t.AssignedToTeam)
                 .WithMany(tm => tm.Tasks)
                 .HasForeignKey(t => t.AssignedToTeamId)
                 .OnDelete(DeleteBehavior.SetNull);

                // Created by User — restrict to avoid cascade conflict
                e.HasOne(t => t.CreatedBy)
                 .WithMany(u => u.CreatedTasks)
                 .HasForeignKey(t => t.CreatedById)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── TaskComment ───────────────────────────────────────────────────
            modelBuilder.Entity<TaskComment>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Comment).HasMaxLength(2000).IsRequired();

                e.HasOne(c => c.Task)
                 .WithMany(t => t.Comments)
                 .HasForeignKey(c => c.TaskId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.User)
                 .WithMany(u => u.Comments)
                 .HasForeignKey(c => c.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── TaskAttachment ────────────────────────────────────────────────
            modelBuilder.Entity<TaskAttachment>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.FileName).HasMaxLength(255).IsRequired();
                e.Property(a => a.FilePath).IsRequired();

                e.HasOne(a => a.Task)
                 .WithMany(t => t.Attachments)
                 .HasForeignKey(a => a.TaskId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(a => a.UploadedByUser)
                 .WithMany()
                 .HasForeignKey(a => a.UploadedByUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Notification ──────────────────────────────────────────────────
            modelBuilder.Entity<Notification>(e =>
            {
                e.HasKey(n => n.Id);
                e.Property(n => n.Message).HasMaxLength(500).IsRequired();
                e.Property(n => n.Type).HasConversion<string>();

                e.HasOne(n => n.User)
                 .WithMany(u => u.Notifications)
                 .HasForeignKey(n => n.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(n => n.Task)
                 .WithMany()
                 .HasForeignKey(n => n.TaskId)
                 .OnDelete(DeleteBehavior.SetNull);
            });
            //-----chat----------------
            modelBuilder.Entity<TeamMessage>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Message).HasMaxLength(2000).IsRequired();

                e.HasOne(m => m.Team)
                 .WithMany()
                 .HasForeignKey(m => m.TeamId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.Sender)
                 .WithMany()
                 .HasForeignKey(m => m.SenderId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
