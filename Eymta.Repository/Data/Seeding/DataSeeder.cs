
using Eymta.core;
using Eymta.core.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eymta.Repository.Data.Seeding
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext ctx)
        {
            // Idempotent — skip if data exists
            if (await ctx.Users.AnyAsync()) return;

            // ── Teams ─────────────────────────────────────────────────
            var teams = new List<Team>
        {
            new() { Name = "Account Management", Color = "#4F46E5" },
            new() { Name = "Marketing",          Color = "#0891B2" },
            new() { Name = "Design",             Color = "#7C3AED" },
            new() { Name = "Admin Coordination", Color = "#059669" }
        };
            await ctx.Teams.AddRangeAsync(teams);
            await ctx.SaveChangesAsync();

            // ── Users ─────────────────────────────────────────────────
            var users = new List<User>
        {
            new() { Username = "admin",      Email = "admin@eymtax.com",    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),    Role = UserRole.Admin,     TeamId = null },
            new() { Username = "ahmed.ali",  Email = "ahmed@eymtax.com",    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"), Role = UserRole.Employee,  TeamId = teams[0].Id },
            new() { Username = "sara.omar",  Email = "sara@eymtax.com",     PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"), Role = UserRole.Employee,  TeamId = teams[0].Id },
            new() { Username = "youssef.k",  Email = "youssef@eymtax.com",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"), Role = UserRole.Employee,  TeamId = teams[1].Id },
            new() { Username = "mona.said",  Email = "mona@eymtax.com",     PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"), Role = UserRole.Employee,  TeamId = teams[1].Id },
            new() { Username = "karim.m",    Email = "karim@eymtax.com",    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"), Role = UserRole.Employee,  TeamId = teams[2].Id },
            new() { Username = "dina.n",     Email = "dina@eymtax.com",     PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"), Role = UserRole.Employee,  TeamId = teams[2].Id },
            new() { Username = "omar.h",     Email = "omar@eymtax.com",     PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"), Role = UserRole.Employee,  TeamId = teams[3].Id }
        };
            await ctx.Users.AddRangeAsync(users);
            await ctx.SaveChangesAsync();

            // Set team leaders
            teams[0].LeaderId = users[1].Id;  // ahmed.ali leads Account Management
            teams[1].LeaderId = users[3].Id;  // youssef leads Marketing
            teams[2].LeaderId = users[5].Id;  // karim leads Design
            teams[3].LeaderId = users[7].Id;  // omar leads Admin Coordination
            await ctx.SaveChangesAsync();

            var admin = users[0];
            var now = DateTime.UtcNow;

            // ── Tasks ─────────────────────────────────────────────────
            var tasks = new List<TaskItem>
        {
            new() {
                Title = "Prepare Q2 Financial Report",
                Description = "Compile all account statements and prepare the quarterly report for management review.",
                Status = TaskStatusEnum.InProgress, Priority = TaskPriority.High,
                StartDate = now.AddDays(-5), DueDate = now.AddDays(3),
                AssignedToUserId = users[1].Id, AssignedToTeamId = teams[0].Id,
                CreatedById = admin.Id, CreatedAt = now.AddDays(-5)
            },
            new() {
                Title = "Client Onboarding — Al-Noor Group",
                Description = "Complete the onboarding process for the new enterprise client.",
                Status = TaskStatusEnum.ToDo, Priority = TaskPriority.High,
                StartDate = now, DueDate = now.AddDays(7),
                AssignedToUserId = users[2].Id, AssignedToTeamId = teams[0].Id,
                CreatedById = admin.Id, CreatedAt = now.AddDays(-1)
            },
            new() {
                Title = "Social Media Campaign — Ramadan",
                Description = "Design and schedule all social media posts for the Ramadan campaign.",
                Status = TaskStatusEnum.InProgress, Priority = TaskPriority.Medium,
                StartDate = now.AddDays(-3), DueDate = now.AddDays(10),
                AssignedToUserId = users[3].Id, AssignedToTeamId = teams[1].Id,
                CreatedById = admin.Id, CreatedAt = now.AddDays(-3)
            },
            new() {
                Title = "Email Newsletter — April Edition",
                Description = "Write and design the monthly email newsletter.",
                Status = TaskStatusEnum.Done, Priority = TaskPriority.Low,
                StartDate = now.AddDays(-15), DueDate = now.AddDays(-2),
                AssignedToUserId = users[4].Id, AssignedToTeamId = teams[1].Id,
                CreatedById = admin.Id, CreatedAt = now.AddDays(-15)
            },
            new() {
                Title = "Redesign Company Website — UI",
                Description = "Create new wireframes and high-fidelity mockups for the website redesign.",
                Status = TaskStatusEnum.InProgress, Priority = TaskPriority.High,
                StartDate = now.AddDays(-7), DueDate = now.AddDays(14),
                AssignedToUserId = users[5].Id, AssignedToTeamId = teams[2].Id,
                CreatedById = admin.Id, CreatedAt = now.AddDays(-7)
            },
            new() {
                Title = "Brand Style Guide Update",
                Description = "Update the brand style guide to include new color palettes and typography.",
                Status = TaskStatusEnum.OnHold, Priority = TaskPriority.Medium,
                StartDate = now.AddDays(-10), DueDate = now.AddDays(20),
                AssignedToUserId = users[6].Id, AssignedToTeamId = teams[2].Id,
                CreatedById = admin.Id, CreatedAt = now.AddDays(-10)
            },
            new() {
                Title = "Employee Attendance System — Integration",
                Description = "Integrate the new attendance system with existing HR software.",
                Status = TaskStatusEnum.ToDo, Priority = TaskPriority.Medium,
                StartDate = now, DueDate = now.AddDays(5),
                AssignedToUserId = users[7].Id, AssignedToTeamId = teams[3].Id,
                CreatedById = admin.Id, CreatedAt = now
            },
            new() {
                Title = "Office Supplies Order — Q2",
                Description = "Place the quarterly order for office supplies and stationery.",
                Status = TaskStatusEnum.Done, Priority = TaskPriority.Low,
                StartDate = now.AddDays(-20), DueDate = now.AddDays(-10),
                AssignedToTeamId = teams[3].Id,
                CreatedById = admin.Id, CreatedAt = now.AddDays(-20)
            }
        };
            await ctx.Tasks.AddRangeAsync(tasks);
            await ctx.SaveChangesAsync();

            // ── Comments ───────────────────────────────────────────────
            var comments = new List<TaskComment>
        {
            new() { TaskId = tasks[0].Id, UserId = users[1].Id, Comment = "Report is 70% complete. Waiting on the finance team to send last invoices.", CreatedAt = now.AddDays(-2) },
            new() { TaskId = tasks[0].Id, UserId = admin.Id, Comment = "Please ensure it's ready before the board meeting on Friday.", CreatedAt = now.AddDays(-1) },
            new() { TaskId = tasks[2].Id, UserId = users[3].Id, Comment = "First batch of 10 posts is ready for review.", CreatedAt = now.AddDays(-1) },
            new() { TaskId = tasks[4].Id, UserId = users[5].Id, Comment = "Homepage mockup is done. Working on the About and Services pages.", CreatedAt = now.AddDays(-2) },
        };
            await ctx.TaskComments.AddRangeAsync(comments);

            // ── Notifications ──────────────────────────────────────────
            var notifications = new List<Notification>
        {
            new() { UserId = users[1].Id, Message = "You have been assigned a new task: Prepare Q2 Financial Report", Type = NotificationType.TaskAssigned, TaskId = tasks[0].Id, IsRead = true },
            new() { UserId = users[2].Id, Message = "You have been assigned a new task: Client Onboarding — Al-Noor Group", Type = NotificationType.TaskAssigned, TaskId = tasks[1].Id, IsRead = false },
            new() { UserId = users[5].Id, Message = "You have been assigned a new task: Redesign Company Website — UI", Type = NotificationType.TaskAssigned, TaskId = tasks[4].Id, IsRead = true },
            new() { UserId = admin.Id, Message = "Task 'Email Newsletter — April Edition' has been marked as Done.", Type = NotificationType.TaskCompleted, TaskId = tasks[3].Id, IsRead = false },
        };
            await ctx.Notifications.AddRangeAsync(notifications);

            await ctx.SaveChangesAsync();
        }
    }
}
