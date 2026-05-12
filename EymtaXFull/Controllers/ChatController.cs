using Eymta.Application.DTO;
using Eymta.core;
using Eymta.core.Enums;
using Eymta.core.Interface;
using Eymta.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EymtaXFull.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [Authorize]
    public class ChatController : BaseController
    {
        private readonly IUnitOfWork _uow;
        private readonly AppDbContext _context;

        public ChatController(IUnitOfWork uow, AppDbContext context)
        {
            _uow = uow;
            _context = context;
        }

        // جلب رسائل فريق معين
        [HttpGet("team/{teamId:int}")]
        public async Task<ActionResult<IEnumerable<TeamMessageDto>>> GetTeamMessages(int teamId, [FromQuery] int count = 50)
        {
            // التحقق إن اليوزر في الفريق ده أو Admin
            var userId = CurrentUserId;
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized();

            bool isMember = user.TeamId == teamId || user.Role == UserRole.Admin;

            if (!isMember)
                return Forbid("You are not a member of this team.");

            var messages = await _context.TeamMessages
                .Include(m => m.Sender)
                .Where(m => m.TeamId == teamId)
                .OrderBy(m => m.SentAt)
                .Take(count)
                .ToListAsync();

            var result = messages.Select(m => new TeamMessageDto
            {
                Id = m.Id,
                TeamId = m.TeamId,
                SenderId = m.SenderId,
                SenderUsername = m.Sender.Username,
                Message = m.Message,
                SentAt = m.SentAt,
                IsFromAdmin = m.Sender.Role == UserRole.Admin
            });

            return Ok(result);
        }

        
        // حذف رسالة (للمرسل أو الـ Admin فقط)
        [HttpDelete("message/{messageId:int}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = CurrentUserId;

            var message = await _context.TeamMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
                return NotFound("الرسالة غير موجودة");

            bool isAdmin = User.IsInRole("Admin");
            bool isSender = message.SenderId == userId;

            if (!isSender && !isAdmin)
                return Forbid("يمكنك حذف رسائلك فقط");

            _uow.TeamMessages.Delete(message);
            await _uow.CompleteAsync();

            return NoContent();
        }
    }
}