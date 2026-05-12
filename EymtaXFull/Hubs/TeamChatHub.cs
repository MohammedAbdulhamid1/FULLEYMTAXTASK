using Eymta.core;
using Eymta.core.Enums;
using Eymta.core.Interface;
using Eymta.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EymtaXFull.Hubs
{
    [Authorize]
    public class TeamChatHub : Hub
    {
        private readonly IUnitOfWork _uow;
        private readonly AppDbContext _context;

        public TeamChatHub(IUnitOfWork uow, AppDbContext context)
        {
            _uow = uow;
            _context = context;
        }

        // ====================== عند الاتصال ======================
        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.TeamId.HasValue == true)
            {
                string groupName = $"Team_{user.TeamId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                await Clients.Group(groupName).SendAsync("UserJoined",
                    new { username = user.Username, teamId = user.TeamId });
            }

            await base.OnConnectedAsync();
        }

        // ====================== عند قطع الاتصال ======================
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user?.TeamId.HasValue == true)
            {
                string groupName = $"Team_{user.TeamId}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ====================== إرسال رسالة داخل الـ Team ======================
        public async Task SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var userId = GetCurrentUserId();
            var user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.TeamId == null)
            {
                await Clients.Caller.SendAsync("ReceiveError", "You are not a member of any team.");
                return;
            }

            string groupName = $"Team_{user.TeamId}";

            var teamMessage = new TeamMessage
            {
                TeamId = user.TeamId.Value,
                SenderId = userId,
                Message = message.Trim(),
                SentAt = DateTime.UtcNow
            };

            await _uow.TeamMessages.AddAsync(teamMessage);
            await _uow.CompleteAsync();

            var messageDto = new
            {
                id = teamMessage.Id,
                teamId = teamMessage.TeamId,
                senderId = teamMessage.SenderId,
                senderUsername = user.Username,
                message = teamMessage.Message,
                sentAt = teamMessage.SentAt,
                isFromAdmin = user.Role == UserRole.Admin
            };

            await Clients.Group(groupName).SendAsync("ReceiveTeamMessage", messageDto);
        }

        // ====================== الـ Admin يرسل لأي Team ======================
        [Authorize(Roles = "Admin")]
        public async Task SendToTeam(int teamId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var adminId = GetCurrentUserId();
            var admin = await _context.Users.FindAsync(adminId);

            if (admin == null) return;

            string groupName = $"Team_{teamId}";

            var teamMessage = new TeamMessage
            {
                TeamId = teamId,
                SenderId = adminId,
                Message = message.Trim(),
                SentAt = DateTime.UtcNow
            };

            await _uow.TeamMessages.AddAsync(teamMessage);
            await _uow.CompleteAsync();

            var messageDto = new
            {
                id = teamMessage.Id,
                teamId = teamMessage.TeamId,
                senderId = teamMessage.SenderId,
                senderUsername = admin.Username,
                message = teamMessage.Message,
                sentAt = teamMessage.SentAt,
                isFromAdmin = true
            };

            await Clients.Group(groupName).SendAsync("ReceiveTeamMessage", messageDto);
        }

        // ====================== جلب رسائل الـ Team ======================
        public async Task GetTeamMessages(int teamId, int count = 50)
        {
            var messages = await _context.TeamMessages
                .Include(m => m.Sender)
                .Where(m => m.TeamId == teamId)
                .OrderBy(m => m.SentAt)
                .Take(count)
                .ToListAsync();

            var messagesDto = messages.Select(m => new
            {
                id = m.Id,
                senderId = m.SenderId,
                senderUsername = m.Sender.Username,
                message = m.Message,
                sentAt = m.SentAt,
                isFromAdmin = m.Sender.Role == UserRole.Admin
            });

            await Clients.Caller.SendAsync("LoadTeamMessages", messagesDto);
        }

        private int GetCurrentUserId()
        {
            var claim = Context.User.FindFirstValue("sub")
                     ?? Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(claim ?? "0");
        }
    }
}