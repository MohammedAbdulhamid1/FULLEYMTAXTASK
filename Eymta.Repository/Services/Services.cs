using AutoMapper;
using Eymta.Application.DTO;
using Eymta.Application.Interfaces;
using Eymta.core;
using Eymta.core.Enums;
using Eymta.core.Interface;
using Eymta.Repository.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting;

namespace Eymta.Repository.Services
{
    // ═══════════════════════════════════════════════════════════════════
    //  TOKEN SERVICE
    // ═══════════════════════════════════════════════════════════════════
    public class TokenService : ITokenService
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _durationDays;

        public TokenService(IConfiguration config)
        {
            _key = config["JWT:Key"]!;
            _issuer = config["JWT:Issuer"]!;
            _audience = config["JWT:Audience"]!;
            _durationDays = int.Parse(config["JWT:DurationInDays"] ?? "7");
        }

        public string GenerateToken(int userId, string username, string role)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new System.Security.Claims.Claim("sub", userId.ToString()),
                new System.Security.Claims.Claim("username", username),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role),
                new System.Security.Claims.Claim("jti", Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_durationDays),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  AUTH SERVICE
    // ═══════════════════════════════════════════════════════════════════
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public AuthService(IUnitOfWork uow, ITokenService tokenService, IMapper mapper)
        {
            _uow = uow;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _uow.Users.FindSingleAsync(u => u.Username == request.Username && u.IsActive);
            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid username or password.");

            var token = _tokenService.GenerateToken(user.Id, user.Username, user.Role.ToString());
            var expires = DateTime.UtcNow.AddDays(7);

            return new LoginResponse(user.Id, user.Username, user.Email, user.Role.ToString(), token, expires);
        }

        public async Task<UserDto> RegisterAsync(RegisterRequest request)
        {
            if (await _uow.Users.ExistsAsync(u => u.Email == request.Email))
                throw new InvalidOperationException("Email already in use.");

            if (await _uow.Users.ExistsAsync(u => u.Username == request.Username))
                throw new InvalidOperationException("Username already taken.");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                TeamId = request.TeamId
            };

            await _uow.Users.AddAsync(user);
            await _uow.CompleteAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _uow.Users.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _uow.Users.Update(user);
            await _uow.CompleteAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  USER SERVICE
    // ═══════════════════════════════════════════════════════════════════
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly AppDbContext _ctx;

        public UserService(IUnitOfWork uow, IMapper mapper, AppDbContext ctx)
        {
            _uow = uow;
            _mapper = mapper;
            _ctx = ctx;
        }

        public async Task<PagedResult<UserDto>> GetAllAsync(int page, int pageSize)
        {
            var query = _ctx.Users.Include(u => u.Team).AsQueryable();
            var total = await query.CountAsync();
            var items = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<UserDto>(
                _mapper.Map<IEnumerable<UserDto>>(items),
                total, page, pageSize,
                (int)Math.Ceiling(total / (double)pageSize));
        }

        public async Task<UserDto> GetByIdAsync(int id)
        {
            var user = await _ctx.Users.Include(u => u.Team).FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new KeyNotFoundException($"User {id} not found.");
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> CreateAsync(CreateUserRequest request)
        {
            if (await _uow.Users.ExistsAsync(u => u.Email == request.Email))
                throw new InvalidOperationException("Email already in use.");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                TeamId = request.TeamId
            };
            await _uow.Users.AddAsync(user);
            await _uow.CompleteAsync();
            return await GetByIdAsync(user.Id);
        }

        public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request)
        {
            var user = await _uow.Users.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"User {id} not found.");

            if (request.Username is not null) user.Username = request.Username;
            if (request.Email is not null) user.Email = request.Email;
            if (request.Role is not null) user.Role = request.Role.Value;
            if (request.TeamId is not null) user.TeamId = request.TeamId;
            if (request.IsActive is not null) user.IsActive = request.IsActive.Value;

            _uow.Users.Update(user);
            await _uow.CompleteAsync();
            return await GetByIdAsync(user.Id);
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _uow.Users.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"User {id} not found.");
            _uow.Users.Delete(user);
            await _uow.CompleteAsync();
        }

        public async Task<IEnumerable<UserDto>> GetByTeamAsync(int teamId)
        {
            var users = await _ctx.Users.Include(u => u.Team)
                .Where(u => u.TeamId == teamId).ToListAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TEAM SERVICE
    // ═══════════════════════════════════════════════════════════════════
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly AppDbContext _ctx;

        public TeamService(IUnitOfWork uow, IMapper mapper, AppDbContext ctx)
        {
            _uow = uow;
            _mapper = mapper;
            _ctx = ctx;
        }

        public async Task<IEnumerable<TeamDto>> GetAllAsync()
        {
            var teams = await _ctx.Teams
                .Include(t => t.Leader)
                .Include(t => t.Members)
                .Include(t => t.Tasks)
                .ToListAsync();
            return _mapper.Map<IEnumerable<TeamDto>>(teams);
        }

        public async Task<TeamDetailDto> GetByIdAsync(int id)
        {
            var team = await _ctx.Teams
                .Include(t => t.Leader)
                .Include(t => t.Members).ThenInclude(m => m.Team)
                .Include(t => t.Tasks)
                .FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new KeyNotFoundException($"Team {id} not found.");
            return _mapper.Map<TeamDetailDto>(team);
        }

        public async Task<TeamDto> CreateAsync(CreateTeamRequest request)
        {
            if (await _uow.Teams.ExistsAsync(t => t.Name == request.Name))
                throw new InvalidOperationException("Team name already exists.");

            var team = new Team { Name = request.Name, Color = request.Color, LeaderId = request.LeaderId };
            await _uow.Teams.AddAsync(team);
            await _uow.CompleteAsync();

            var saved = await _ctx.Teams
                .Include(t => t.Leader)
                .Include(t => t.Members)
                .Include(t => t.Tasks)
                .FirstAsync(t => t.Id == team.Id);
            return _mapper.Map<TeamDto>(saved);
        }

        public async Task<TeamDto> UpdateAsync(int id, UpdateTeamRequest request)
        {
            var team = await _uow.Teams.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Team {id} not found.");

            if (request.Name is not null) team.Name = request.Name;
            if (request.Color is not null) team.Color = request.Color;
            if (request.LeaderId is not null) team.LeaderId = request.LeaderId;

            _uow.Teams.Update(team);
            await _uow.CompleteAsync();
            return (await GetAllAsync()).First(t => t.Id == id);
        }

        public async Task DeleteAsync(int id)
        {
            var team = await _uow.Teams.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Team {id} not found.");
            _uow.Teams.Delete(team);
            await _uow.CompleteAsync();
        }

        public async Task AssignMemberAsync(int teamId, int userId)
        {
            if (!await _uow.Teams.ExistsAsync(t => t.Id == teamId))
                throw new KeyNotFoundException($"Team {teamId} not found.");

            var user = await _uow.Users.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User {userId} not found.");

            user.TeamId = teamId;
            _uow.Users.Update(user);
            await _uow.CompleteAsync();
        }

        public async Task RemoveMemberAsync(int teamId, int userId)
        {
            var user = await _uow.Users.FindSingleAsync(u => u.Id == userId && u.TeamId == teamId)
                ?? throw new KeyNotFoundException("User is not a member of this team.");

            user.TeamId = null;
            _uow.Users.Update(user);
            await _uow.CompleteAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TASK SERVICE
    // ═══════════════════════════════════════════════════════════════════
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly AppDbContext _ctx;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _env;

        public TaskService(IUnitOfWork uow, IMapper mapper, AppDbContext ctx,
            INotificationService notificationService, IWebHostEnvironment env)
        {
            _uow = uow;
            _mapper = mapper;
            _ctx = ctx;
            _notificationService = notificationService;
            _env = env;
        }

        private IQueryable<TaskItem> TasksWithIncludes() =>
            _ctx.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.AssignedToTeam)
                .Include(t => t.CreatedBy)
                .Include(t => t.Comments)
                .Include(t => t.Attachments);

        public async Task<PagedResult<TaskDto>> GetAllAsync(TaskFilterParams f)
        {
            var q = TasksWithIncludes().AsQueryable();

            if (f.TeamId.HasValue) q = q.Where(t => t.AssignedToTeamId == f.TeamId);
            if (f.AssignedUserId.HasValue) q = q.Where(t => t.AssignedToUserId == f.AssignedUserId);
            if (f.Status.HasValue) q = q.Where(t => t.Status == f.Status.Value);
            if (f.Priority.HasValue) q = q.Where(t => t.Priority == f.Priority.Value);
            if (f.DueBefore.HasValue) q = q.Where(t => t.DueDate <= f.DueBefore.Value);
            if (f.DueAfter.HasValue) q = q.Where(t => t.DueDate >= f.DueAfter.Value);
            if (!string.IsNullOrWhiteSpace(f.Search))
                q = q.Where(t => t.Title.Contains(f.Search) ||
                                 (t.Description != null && t.Description.Contains(f.Search)));

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(t => t.CreatedAt)
                .Skip((f.Page - 1) * f.PageSize)
                .Take(f.PageSize)
                .ToListAsync();

            return new PagedResult<TaskDto>(
                _mapper.Map<IEnumerable<TaskDto>>(items),
                total, f.Page, f.PageSize,
                (int)Math.Ceiling(total / (double)f.PageSize));
        }

        public async Task<TaskDto> GetByIdAsync(int id)
        {
            var task = await TasksWithIncludes().FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new KeyNotFoundException($"Task {id} not found.");
            return _mapper.Map<TaskDto>(task);
        }

        public async Task<TaskDto> CreateAsync(CreateTaskRequest request, int createdByUserId)
        {
            if (createdByUserId <= 0)
                throw new UnauthorizedAccessException("Invalid user ID. Please login again.");

            // اختياري: تأكد إن اليوزر موجود فعلاً
            if (!await _ctx.Users.AnyAsync(u => u.Id == createdByUserId))
                throw new InvalidOperationException($"User with ID {createdByUserId} does not exist.");

            var task = new TaskItem
            {
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                AssignedToUserId = request.AssignedToUserId,
                AssignedToTeamId = request.AssignedToTeamId,
                CreatedById = createdByUserId,        // ← هنا كان المشكلة
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Tasks.AddAsync(task);
            await _uow.CompleteAsync();

            


            if (task.AssignedToUserId.HasValue)
            {
                await _notificationService.SendAsync(
                    task.AssignedToUserId.Value,
                    $"You have been assigned a new task: {task.Title}",
                    new NotificationTypeWrapper(NotificationType.TaskAssigned),
                    task.Id);
            }

            return await GetByIdAsync(task.Id);
        }

        public async Task<TaskDto> UpdateAsync(int id, UpdateTaskRequest request)
        {
            var task = await _uow.Tasks.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Task {id} not found.");

            if (request.Title is not null) task.Title = request.Title;
            if (request.Description is not null) task.Description = request.Description;
            if (request.Priority is not null) task.Priority = request.Priority.Value;
            if (request.DueDate is not null) task.DueDate = request.DueDate.Value;
            if (request.AssignedToUserId is not null) task.AssignedToUserId = request.AssignedToUserId;
            if (request.AssignedToTeamId is not null) task.AssignedToTeamId = request.AssignedToTeamId;

            _uow.Tasks.Update(task);
            await _uow.CompleteAsync();
            return await GetByIdAsync(id);
        }

        public async Task<TaskDto> UpdateStatusAsync(int id, UpdateTaskStatusRequest request, int updatedByUserId)
        {
            var task = await _uow.Tasks.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Task {id} not found.");

            task.Status = request.Status;
            _uow.Tasks.Update(task);
            await _uow.CompleteAsync();

            if (request.Status == TaskStatusEnum.Done && task.CreatedById != updatedByUserId)
            {
                await _notificationService.SendAsync(
                    task.CreatedById,
                    $"Task '{task.Title}' has been marked as Done.",
                    new NotificationTypeWrapper(NotificationType.TaskCompleted),
                    task.Id);
            }

            return await GetByIdAsync(id);
        }

        public async Task DeleteAsync(int id)
        {
            var task = await _uow.Tasks.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Task {id} not found.");
            _uow.Tasks.Delete(task);
            await _uow.CompleteAsync();
        }

        // ── Comments ──────────────────────────────────────────────────
        public async Task<IEnumerable<TaskCommentDto>> GetCommentsAsync(int taskId)
        {
            var comments = await _ctx.TaskComments
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
            return _mapper.Map<IEnumerable<TaskCommentDto>>(comments);
        }

        public async Task<TaskCommentDto> AddCommentAsync(int taskId, AddCommentRequest request, int userId)
        {
            if (!await _uow.Tasks.ExistsAsync(t => t.Id == taskId))
                throw new KeyNotFoundException($"Task {taskId} not found.");

            var comment = new TaskComment { TaskId = taskId, UserId = userId, Comment = request.Comment };
            await _uow.TaskComments.AddAsync(comment);
            await _uow.CompleteAsync();

            var task = await _uow.Tasks.GetByIdAsync(taskId);
            if (task is not null && task.CreatedById != userId)
            {
                await _notificationService.SendAsync(
                    task.CreatedById,
                    $"New comment on task '{task.Title}'.",
                    new NotificationTypeWrapper(NotificationType.TaskCommented),
                    taskId);
            }

            return _mapper.Map<TaskCommentDto>(
                await _ctx.TaskComments.Include(c => c.User).FirstAsync(c => c.Id == comment.Id));
        }

        public async Task DeleteCommentAsync(int commentId, int requestingUserId)
        {
            var comment = await _uow.TaskComments.GetByIdAsync(commentId)
                ?? throw new KeyNotFoundException("Comment not found.");

            if (comment.UserId != requestingUserId)
                throw new UnauthorizedAccessException("You can only delete your own comments.");

            _uow.TaskComments.Delete(comment);
            await _uow.CompleteAsync();
        }

        // ── Attachments ───────────────────────────────────────────────
        public async Task<IEnumerable<AttachmentDto>> GetAttachmentsAsync(int taskId)
        {
            var attachments = await _ctx.TaskAttachments
                .Include(a => a.UploadedByUser)
                .Where(a => a.TaskId == taskId)
                .ToListAsync();
            return _mapper.Map<IEnumerable<AttachmentDto>>(attachments);
        }

        public async Task<AttachmentDto> UploadAttachmentAsync(int taskId, IFormFile file, int userId)
        {
            if (!await _uow.Tasks.ExistsAsync(t => t.Id == taskId))
                throw new KeyNotFoundException($"Task {taskId} not found.");

            var allowedTypes = new[]
            {
                "image/jpeg", "image/png", "image/gif", "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "text/plain"
            };

            if (!allowedTypes.Contains(file.ContentType))
                throw new InvalidOperationException("File type not allowed.");

            if (file.Length > 10 * 1024 * 1024)
                throw new InvalidOperationException("File size exceeds 10MB limit.");

            var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", taskId.ToString());
            Directory.CreateDirectory(uploadsDir);

            var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(uploadsDir, uniqueName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            var relativePath = Path.Combine("uploads", taskId.ToString(), uniqueName).Replace("\\", "/");

            var attachment = new TaskAttachment
            {
                TaskId = taskId,
                FileName = file.FileName,
                FilePath = relativePath,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                UploadedByUserId = userId
            };

            await _uow.TaskAttachments.AddAsync(attachment);
            await _uow.CompleteAsync();

            return _mapper.Map<AttachmentDto>(
                await _ctx.TaskAttachments.Include(a => a.UploadedByUser).FirstAsync(a => a.Id == attachment.Id));
        }

        public async Task DeleteAttachmentAsync(int attachmentId, int requestingUserId)
        {
            var attachment = await _ctx.TaskAttachments
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == attachmentId)
                ?? throw new KeyNotFoundException("Attachment not found.");

            _uow.TaskAttachments.Delete(attachment);
            await _uow.CompleteAsync();

            var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", attachment.FilePath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DASHBOARD SERVICE
    // ═══════════════════════════════════════════════════════════════════
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _ctx;
        private readonly IMapper _mapper;

        public DashboardService(AppDbContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        public async Task<DashboardDto> GetDashboardAsync(int? teamId = null)
        {
            var taskQuery = _ctx.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.AssignedToTeam)
                .Include(t => t.CreatedBy)
                .Include(t => t.Comments)
                .Include(t => t.Attachments)
                .AsQueryable();

            if (teamId.HasValue)
                taskQuery = taskQuery.Where(t => t.AssignedToTeamId == teamId);

            var allTasks = await taskQuery.ToListAsync();
            var now = DateTime.UtcNow;

            var teamsRaw = await _ctx.Teams
                .Include(t => t.Tasks)
                .ToListAsync();

            // ✅ مقارنة الـ enum مباشرة بدل .ToString()
            var byTeam = teamsRaw.Select(t => new TeamTaskStats
            {
                TeamId = t.Id,
                TeamName = t.Name,
                TeamColor = t.Color,
                Total = t.Tasks?.Count ?? 0,
                ToDo = t.Tasks?.Count(x => x.Status == TaskStatusEnum.ToDo) ?? 0,
                InProgress = t.Tasks?.Count(x => x.Status == TaskStatusEnum.InProgress) ?? 0,
                Done = t.Tasks?.Count(x => x.Status == TaskStatusEnum.Done) ?? 0,
                OnHold = t.Tasks?.Count(x => x.Status == TaskStatusEnum.OnHold) ?? 0,
            }).ToList();

            var recent = allTasks.OrderByDescending(t => t.CreatedAt).Take(10);

            // ✅ object initializer بدل constructor
            return new DashboardDto
            {
                TotalTasks = allTasks.Count,
                ToDo = allTasks.Count(t => t.Status == TaskStatusEnum.ToDo),
                InProgress = allTasks.Count(t => t.Status == TaskStatusEnum.InProgress),
                Done = allTasks.Count(t => t.Status == TaskStatusEnum.Done),
                OnHold = allTasks.Count(t => t.Status == TaskStatusEnum.OnHold),
                Overdue = allTasks.Count(t => t.DueDate < now && t.Status != TaskStatusEnum.Done),
                ByTeam = byTeam,
                RecentTasks = _mapper.Map<IEnumerable<TaskDto>>(recent)
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  NOTIFICATION SERVICE
    // ═══════════════════════════════════════════════════════════════════
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly AppDbContext _ctx;
        private readonly IMapper _mapper;

        public NotificationService(IUnitOfWork uow, AppDbContext ctx, IMapper mapper)
        {
            _uow = uow;
            _ctx = ctx;
            _mapper = mapper;
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId)
        {
            var notifs = await _ctx.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();
            return _mapper.Map<IEnumerable<NotificationDto>>(notifs);
        }

        public async Task<int> GetUnreadCountAsync(int userId) =>
            await _uow.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notif = await _uow.Notifications.FindSingleAsync(n => n.Id == notificationId && n.UserId == userId)
                ?? throw new KeyNotFoundException("Notification not found.");
            notif.IsRead = true;
            _uow.Notifications.Update(notif);
            await _uow.CompleteAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifs = await _uow.Notifications.FindAsync(n => n.UserId == userId && !n.IsRead);
            foreach (var n in notifs) n.IsRead = true;
            await _uow.CompleteAsync();
        }

        public async Task SendAsync(int userId, string message, NotificationTypeWrapper type, int? taskId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Type = type.Value,
                TaskId = taskId
            };
            await _uow.Notifications.AddAsync(notification);
            await _uow.CompleteAsync();
        }
    }
}