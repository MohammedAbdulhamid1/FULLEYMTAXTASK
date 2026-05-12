using Eymta.Application.DTO;
using Eymta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EymtaXFull.Controllers
{
    [Route("api/[controller]")]
    [ApiController]


    // ═══════════════════════════════════════════════════════════════════
    //  BASE CONTROLLER
    // ═══════════════════════════════════════════════════════════════
   
    public abstract class BaseController : ControllerBase
    {
        protected int CurrentUserId
        {
            get
            {
                // جرب كل الـ claims الشائعة
                var claim = User.FindFirstValue("sub")
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

                if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int userId) || userId <= 0)
                {
                    // للـ Debug فقط (شيلها بعد ما تخلص)
                    Console.WriteLine("=== CurrentUserId Debug ===");
                    Console.WriteLine("All Claims: " + string.Join(" | ", User.Claims.Select(c => $"{c.Type} = {c.Value}")));

                    throw new UnauthorizedAccessException("User is not authenticated properly. Please login again.");
                }

                return userId;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  AUTH CONTROLLER
    // ═══════════════════════════════════════════════════════════════════
    [Route("api/auth")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService) => _authService = authService;

        /// <summary>Login and receive JWT token</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

        /// <summary>Change password for authenticated user</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            await _authService.ChangePasswordAsync(CurrentUserId, request);
            return NoContent();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  USER CONTROLLER
    // ═══════════════════════════════════════════════════════════════════
    [Authorize]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService) => _userService = userService;

        /// <summary>Get all users (Admin only)</summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return Ok(await _userService.GetAllAsync(page, pageSize));
        }

        /// <summary>Get user by ID</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetById(int id) =>
            Ok(await _userService.GetByIdAsync(id));

        /// <summary>Get users by team</summary>
        [HttpGet("team/{teamId:int}")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetByTeam(int teamId) =>
            Ok(await _userService.GetByTeamAsync(teamId));

        /// <summary>Create a new user (Admin only)</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
        {
            var user = await _userService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        /// <summary>Update user (Admin only)</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest request) =>
            Ok(await _userService.UpdateAsync(id, request));

        /// <summary>Delete user (Admin only)</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>Get current user profile</summary>
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> Me() =>
            Ok(await _userService.GetByIdAsync(CurrentUserId));

        [HttpGet("online-status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetOnlineStatus()
        {
            var users = await _userService.GetAllAsync(1, 100);
            return Ok(users.Items);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TEAM CONTROLLER
    // ═══════════════════════════════════════════════════════════════════
    [Authorize]
    public class TeamController : BaseController
    {
        private readonly ITeamService _teamService;
        public TeamController(ITeamService teamService) => _teamService = teamService;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeamDto>>> GetAll() =>
            Ok(await _teamService.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TeamDetailDto>> GetById(int id) =>
            Ok(await _teamService.GetByIdAsync(id));

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TeamDto>> Create([FromBody] CreateTeamRequest request)
        {
            var team = await _teamService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = team.Id }, team);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TeamDto>> Update(int id, [FromBody] UpdateTeamRequest request) =>
            Ok(await _teamService.UpdateAsync(id, request));

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _teamService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{teamId:int}/members")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignMember(int teamId, [FromBody] AssignMemberRequest request)
        {
            await _teamService.AssignMemberAsync(teamId, request.UserId);
            return NoContent();
        }

        [HttpDelete("{teamId:int}/members/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveMember(int teamId, int userId)
        {
            await _teamService.RemoveMemberAsync(teamId, userId);
            return NoContent();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TASK CONTROLLER
    // ═══════════════════════════════════════════════════════════════════
    [Authorize]
    public class TaskController : BaseController
    {
        private readonly ITaskService _taskService;
        public TaskController(ITaskService taskService) => _taskService = taskService;

        /// <summary>Get all tasks with optional filters</summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<TaskDto>>> GetAll([FromQuery] TaskFilterParams filters) =>
            Ok(await _taskService.GetAllAsync(filters));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TaskDto>> GetById(int id) =>
            Ok(await _taskService.GetByIdAsync(id));

        [HttpPost]
        [Authorize]   // أي موظف مسجل يقدر ينشئ task
        public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest request)
        {
            try
            {
                var task = await _taskService.CreateAsync(request, CurrentUserId);
                return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create Task Error for user {CurrentUserId}: {ex.Message}");
                return BadRequest("حدث خطأ أثناء إنشاء المهمة. حاول مرة أخرى.");
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TaskDto>> Update(int id, [FromBody] UpdateTaskRequest request) =>
            Ok(await _taskService.UpdateAsync(id, request));

        /// <summary>Update task status — available to both Admin and Employee</summary>
        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<TaskDto>> UpdateStatus(int id, [FromBody] UpdateTaskStatusRequest request)
        {
            // Admin يقدر يغير أي task
            if (User.IsInRole("Admin"))
                return Ok(await _taskService.UpdateStatusAsync(id, request, CurrentUserId));

            // Employee يتحقق إنه هو المعين على الـ task دي
            var task = await _taskService.GetByIdAsync(id);

            if (task.AssignedToUserId != CurrentUserId)
                return Forbid();  // مش شغال عليها

            return Ok(await _taskService.UpdateStatusAsync(id, request, CurrentUserId));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _taskService.DeleteAsync(id);
            return NoContent();
        }

        // ── Comments ────────────────────────────────────────────────
        [HttpGet("{taskId:int}/comments")]
        public async Task<ActionResult<IEnumerable<TaskCommentDto>>> GetComments(int taskId) =>
            Ok(await _taskService.GetCommentsAsync(taskId));

        [HttpPost("{taskId:int}/comments")]
        public async Task<ActionResult<TaskCommentDto>> AddComment(int taskId, [FromBody] AddCommentRequest request) =>
            Ok(await _taskService.AddCommentAsync(taskId, request, CurrentUserId));

        [HttpDelete("{taskId:int}/comments/{commentId:int}")]
        public async Task<IActionResult> DeleteComment(int taskId, int commentId)
        {
            await _taskService.DeleteCommentAsync(commentId, CurrentUserId);
            return NoContent();
        }

        // ── Attachments ─────────────────────────────────────────────
        [HttpGet("{taskId:int}/attachments")]
        public async Task<ActionResult<IEnumerable<AttachmentDto>>> GetAttachments(int taskId) =>
            Ok(await _taskService.GetAttachmentsAsync(taskId));

        [HttpPost("{taskId:int}/attachments")]
        public async Task<ActionResult<AttachmentDto>> Upload(int taskId, IFormFile file) =>
            Ok(await _taskService.UploadAttachmentAsync(taskId, file, CurrentUserId));

        [HttpDelete("{taskId:int}/attachments/{attachmentId:int}")]
        public async Task<IActionResult> DeleteAttachment(int taskId, int attachmentId)
        {
            await _taskService.DeleteAttachmentAsync(attachmentId, CurrentUserId);
            return NoContent();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DASHBOARD CONTROLLER
    // ═══════════════════════════════════════════════════════════════════
    [Authorize]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;
        public DashboardController(IDashboardService dashboardService) => _dashboardService = dashboardService;

        [HttpGet]
        public async Task<ActionResult<DashboardDto>> GetDashboard([FromQuery] int? teamId = null) =>
            Ok(await _dashboardService.GetDashboardAsync(teamId));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  NOTIFICATION CONTROLLER
    // ═══════════════════════════════════════════════════════════════════
    [Authorize]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notifService;
        public NotificationController(INotificationService notifService) => _notifService = notifService;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetMine() =>
            Ok(await _notifService.GetUserNotificationsAsync(CurrentUserId));

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> UnreadCount() =>
            Ok(await _notifService.GetUnreadCountAsync(CurrentUserId));

        [HttpPatch("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _notifService.MarkAsReadAsync(id, CurrentUserId);
            return NoContent();
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            await _notifService.MarkAllAsReadAsync(CurrentUserId);
            return NoContent();
        }
    }

}
