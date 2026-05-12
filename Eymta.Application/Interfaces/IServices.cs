using Eymta.Application.DTO;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eymta.Application.Interfaces
{


    // ─────────────────────────────────────────────
    //  Auth
    // ─────────────────────────────────────────────
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<UserDto> RegisterAsync(RegisterRequest request);
        Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    }

    // ─────────────────────────────────────────────
    //  Token
    // ─────────────────────────────────────────────
    public interface ITokenService
    {
        string GenerateToken(int userId, string username, string role);
    }

    // ─────────────────────────────────────────────
    //  User
    // ─────────────────────────────────────────────
    public interface IUserService
    {
        Task<PagedResult<UserDto>> GetAllAsync(int page, int pageSize);
        Task<UserDto> GetByIdAsync(int id);
        Task<UserDto> CreateAsync(CreateUserRequest request);
        Task<UserDto> UpdateAsync(int id, UpdateUserRequest request);
        Task DeleteAsync(int id);
        Task<IEnumerable<UserDto>> GetByTeamAsync(int teamId);
    }

    // ─────────────────────────────────────────────
    //  Team
    // ─────────────────────────────────────────────
    public interface ITeamService
    {
        Task<IEnumerable<TeamDto>> GetAllAsync();
        Task<TeamDetailDto> GetByIdAsync(int id);
        Task<TeamDto> CreateAsync(CreateTeamRequest request);
        Task<TeamDto> UpdateAsync(int id, UpdateTeamRequest request);
        Task DeleteAsync(int id);
        Task AssignMemberAsync(int teamId, int userId);
        Task RemoveMemberAsync(int teamId, int userId);
    }

    // ─────────────────────────────────────────────
    //  Task
    // ─────────────────────────────────────────────
    public interface ITaskService
    {
        Task<PagedResult<TaskDto>> GetAllAsync(TaskFilterParams filters);
        Task<TaskDto> GetByIdAsync(int id);
        Task<TaskDto> CreateAsync(CreateTaskRequest request, int createdByUserId);
        Task<TaskDto> UpdateAsync(int id, UpdateTaskRequest request);
        Task<TaskDto> UpdateStatusAsync(int id, UpdateTaskStatusRequest request, int updatedByUserId);
        Task DeleteAsync(int id);

        // Comments
        Task<IEnumerable<TaskCommentDto>> GetCommentsAsync(int taskId);
        Task<TaskCommentDto> AddCommentAsync(int taskId, AddCommentRequest request, int userId);
        Task DeleteCommentAsync(int commentId, int requestingUserId);

        // Attachments
        Task<IEnumerable<AttachmentDto>> GetAttachmentsAsync(int taskId);
        Task<AttachmentDto> UploadAttachmentAsync(int taskId, IFormFile file, int userId);
        Task DeleteAttachmentAsync(int attachmentId, int requestingUserId);
    }

    // ─────────────────────────────────────────────
    //  Dashboard
    // ─────────────────────────────────────────────
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(int? teamId = null);
    }

    // ─────────────────────────────────────────────
    //  Notification
    // ─────────────────────────────────────────────
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task SendAsync(int userId, string message, NotificationTypeWrapper type, int? taskId = null);
    }

    public record NotificationTypeWrapper(Eymta.core.Enums.NotificationType Value);

}
