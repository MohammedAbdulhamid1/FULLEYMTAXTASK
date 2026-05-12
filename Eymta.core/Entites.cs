using Eymta.core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eymta.core
{
  


        // ─────────────────────────────────────────────
        //  Team
        // ─────────────────────────────────────────────
        public class Team : BaseEntity
        {
            public string Name { get; set; } = string.Empty;
            public string? Color { get; set; }           // hex, e.g. "#4F46E5"
            public int? LeaderId { get; set; }

            // Navigation
            public User? Leader { get; set; }
            public ICollection<User> Members { get; set; } = new List<User>();
            public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        }

        // ─────────────────────────────────────────────
        //  User / Employee
        // ─────────────────────────────────────────────
        public class User : BaseEntity
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public UserRole Role { get; set; } = UserRole.Employee;
            public bool IsActive { get; set; } = true;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public int? TeamId { get; set; }

            // Navigation
            public Team? Team { get; set; }
            public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
            public ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
            public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
            public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public DateTime? LastSeenAt { get; set; }

        // Computed — مش محتاج تحفظها في DB
        public bool IsOnline => LastSeenAt.HasValue &&
                                (DateTime.UtcNow - LastSeenAt.Value).TotalMinutes < 5;
    }

        // ─────────────────────────────────────────────
        //  Task
        // ─────────────────────────────────────────────
        public class TaskItem : BaseEntity
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public TaskStatusEnum Status { get; set; } = TaskStatusEnum.ToDo;
            public TaskPriority Priority { get; set; } = TaskPriority.Medium;
            public DateTime StartDate { get; set; } = DateTime.UtcNow;
            public DateTime DueDate { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            // Assignment — either a specific user OR a whole team
            public int? AssignedToUserId { get; set; }
            public int? AssignedToTeamId { get; set; }

            public int CreatedById { get; set; }

            // Navigation
            public User? AssignedToUser { get; set; }
            public Team? AssignedToTeam { get; set; }
            public User CreatedBy { get; set; } = null!;
            public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
            public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        }

        // ─────────────────────────────────────────────
        //  TaskComment
        // ─────────────────────────────────────────────
        public class TaskComment : BaseEntity
        {
            public string Comment { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public int TaskId { get; set; }
            public int UserId { get; set; }

            // Navigation
            public TaskItem Task { get; set; } = null!;
            public User User { get; set; } = null!;
        }

        // ─────────────────────────────────────────────
        //  TaskAttachment
        // ─────────────────────────────────────────────
        public class TaskAttachment : BaseEntity
        {
            public string FileName { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            public string? ContentType { get; set; }
            public long FileSizeBytes { get; set; }
            public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

            public int TaskId { get; set; }
            public int UploadedByUserId { get; set; }

            // Navigation
            public TaskItem Task { get; set; } = null!;
            public User UploadedByUser { get; set; } = null!;
        }

        // ─────────────────────────────────────────────
        //  Notification
        // ─────────────────────────────────────────────
        public class Notification : BaseEntity
        {
            public string Message { get; set; } = string.Empty;
            public NotificationType Type { get; set; }
            public bool IsRead { get; set; } = false;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public int UserId { get; set; }
            public int? TaskId { get; set; }

            // Navigation
            public User User { get; set; } = null!;
            public TaskItem? Task { get; set; }
        }
    /// <summary>
    /// /chat team
    /// </summary>
    public class TeamMessage : BaseEntity
    {
        public int TeamId { get; set; }
        public int SenderId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Team Team { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }

}

