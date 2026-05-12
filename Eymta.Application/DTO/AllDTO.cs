using Eymta.core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eymta.Application.DTO
{


    // ════════════════════════════════════════════
    //  AUTH
    // ════════════════════════════════════════════

    public record LoginRequest(string Username, string Password);

    public record LoginResponse(
        int UserId,
        string Username,
        string Email,
        string Role,
        string Token,
        DateTime ExpiresAt
    );

    public record RegisterRequest(
        string Username,
        string Email,
        string Password,
        UserRole Role = UserRole.Employee,
        int? TeamId = null
    );

    public record ChangePasswordRequest(
        string CurrentPassword,
        string NewPassword,
        string ConfirmPassword
    );

    // ════════════════════════════════════════════
    //  USER
    // ════════════════════════════════════════════

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public bool IsOnline { get; set; }
    }

    public record CreateUserRequest(
        string Username,
        string Email,
        string Password,
        UserRole Role,
        int? TeamId
    );

    public record UpdateUserRequest(
        string? Username,
        string? Email,
        UserRole? Role,
        int? TeamId,
        bool? IsActive
    );

    // ════════════════════════════════════════════
    //  TEAM
    // ════════════════════════════════════════════

    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public int? LeaderId { get; set; }
        public string? LeaderName { get; set; }
        public int MembersCount { get; set; }
        public int ActiveTasksCount { get; set; }
    }

    public class TeamDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public int? LeaderId { get; set; }
        public string? LeaderName { get; set; }
        public IEnumerable<UserDto> Members { get; set; } = new List<UserDto>();
        public IEnumerable<TaskSummaryDto> Tasks { get; set; } = new List<TaskSummaryDto>();
    }

    public record CreateTeamRequest(string Name, string? Color, int? LeaderId);

    public record UpdateTeamRequest(string? Name, string? Color, int? LeaderId);

    public record AssignMemberRequest(int UserId);

    // ════════════════════════════════════════════
    //  TASK
    // ════════════════════════════════════════════

    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public int? AssignedToTeamId { get; set; }
        public string? AssignedToTeamName { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class TaskSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsOverdue { get; set; }
    }

    public record CreateTaskRequest(
        string Title,
        string? Description,
        TaskPriority Priority,
        DateTime StartDate,
        DateTime DueDate,
        int? AssignedToUserId,
        int? AssignedToTeamId
    );

    public record UpdateTaskRequest(
        string? Title,
        string? Description,
        TaskPriority? Priority,
        DateTime? DueDate,
        int? AssignedToUserId,
        int? AssignedToTeamId
    );

    public record UpdateTaskStatusRequest(TaskStatusEnum Status);

    public record TaskFilterParams(
        int? TeamId = null,
        int? AssignedUserId = null,
        TaskStatusEnum? Status = null,
        TaskPriority? Priority = null,
        DateTime? DueBefore = null,
        DateTime? DueAfter = null,
        string? Search = null,
        int Page = 1,
        int PageSize = 20
    );

    // ════════════════════════════════════════════
    //  COMMENT
    // ════════════════════════════════════════════

    public class TaskCommentDto
    {
        public int Id { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public record AddCommentRequest(string Comment);

    // ════════════════════════════════════════════
    //  ATTACHMENT
    // ════════════════════════════════════════════

    public class AttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
    }

    // ════════════════════════════════════════════
    //  NOTIFICATION
    // ════════════════════════════════════════════

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? TaskId { get; set; }
    }

    // ════════════════════════════════════════════
    //  DASHBOARD / ANALYTICS
    // ════════════════════════════════════════════

    public class DashboardDto
    {
        public int TotalTasks { get; set; }
        public int ToDo { get; set; }
        public int InProgress { get; set; }
        public int Done { get; set; }
        public int OnHold { get; set; }
        public int Overdue { get; set; }
        public IEnumerable<TeamTaskStats> ByTeam { get; set; } = new List<TeamTaskStats>();
        public IEnumerable<TaskDto> RecentTasks { get; set; } = new List<TaskDto>();
    }

    public class TeamTaskStats
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? TeamColor { get; set; }
        public int Total { get; set; }
        public int ToDo { get; set; }
        public int InProgress { get; set; }
        public int Done { get; set; }
        public int OnHold { get; set; }
    }

    // ════════════════════════════════════════════
    //  PAGINATION
    // ════════════════════════════════════════════

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public PagedResult() { }

        public PagedResult(IEnumerable<T> items, int totalCount, int page, int pageSize, int totalPages)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
            TotalPages = totalPages;
        }
    }
    // ─────────────────────────────────────────────
    // TEAM CHAT DTOs
    // ─────────────────────────────────────────────

    public class TeamMessageDto
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int SenderId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsFromAdmin { get; set; } = false;
    }

    public class SendTeamMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class AdminSendToTeamRequest
    {
        public int TeamId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

}
