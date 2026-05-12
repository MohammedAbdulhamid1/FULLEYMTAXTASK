using AutoMapper;
using Eymta.Application.DTO;
using Eymta.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Eymta.Application.Mappings
{


    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ── User ──────────────────────────────
            CreateMap<User, UserDto>()
                .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()))
                .ForMember(d => d.TeamName, o => o.MapFrom(s => s.Team != null ? s.Team.Name : null))
                .ForMember(d => d.IsOnline, o => o.MapFrom(s =>
        s.LastSeenAt.HasValue &&
        (DateTime.UtcNow - s.LastSeenAt.Value).TotalMinutes < 5))
    .ForMember(d => d.LastSeenAt, o => o.MapFrom(s => s.LastSeenAt)); ;

            // ── Team ──────────────────────────────
            CreateMap<Team, TeamDto>()
      .ForMember(d => d.LeaderName, o => o.MapFrom(s =>
          s.Leader != null ? s.Leader.Username : null))
      .ForMember(d => d.MembersCount, o => o.MapFrom(s =>
          s.Members == null ? 0 : s.Members.Count))
      .ForMember(d => d.ActiveTasksCount, o => o.MapFrom(s =>
          s.Tasks == null ? 0 : s.Tasks.Count(t => t.Status != core.Enums.TaskStatusEnum.Done)));

            CreateMap<Team, TeamDetailDto>()
                .ForMember(d => d.LeaderName, o => o.MapFrom(s => s.Leader != null ? s.Leader.Username : null))
                .ForMember(d => d.Members, o => o.MapFrom(s => s.Members))
                .ForMember(d => d.Tasks, o => o.MapFrom(s => s.Tasks));

            // ── TaskItem ──────────────────────────
            CreateMap<TaskItem, TaskDto>()
      .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
      .ForMember(d => d.Priority, o => o.MapFrom(s => s.Priority.ToString()))
      .ForMember(d => d.AssignedToUserName, o => o.MapFrom(s => s.AssignedToUser != null ? s.AssignedToUser.Username : null))
      .ForMember(d => d.AssignedToTeamName, o => o.MapFrom(s => s.AssignedToTeam != null ? s.AssignedToTeam.Name : null))
      .ForMember(d => d.CreatedByName, o => o.MapFrom(s => s.CreatedBy != null ? s.CreatedBy.Username : null))
      .ForMember(d => d.CommentsCount, o => o.MapFrom(s => s.Comments != null ? s.Comments.Count : 0))
      .ForMember(d => d.AttachmentsCount, o => o.MapFrom(s => s.Attachments != null ? s.Attachments.Count : 0))
      .ForMember(d => d.IsOverdue, o => o.MapFrom(s => s.DueDate < DateTime.UtcNow && s.Status != core.Enums.TaskStatusEnum.Done));

            CreateMap<TaskItem, TaskSummaryDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.Priority, o => o.MapFrom(s => s.Priority.ToString()))
                .ForMember(d => d.IsOverdue, o => o.MapFrom(s =>
                    s.DueDate < DateTime.UtcNow && s.Status != core.Enums.TaskStatusEnum.Done));

            // ── Comment ───────────────────────────
            CreateMap<TaskComment, TaskCommentDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.Username));

            // ── Attachment ────────────────────────
            CreateMap<TaskAttachment, AttachmentDto>()
                .ForMember(d => d.UploadedByName, o => o.MapFrom(s => s.UploadedByUser.Username));

            // ── Notification ──────────────────────
            CreateMap<Notification, NotificationDto>()
                .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));
            //------chat team---------------------
            CreateMap<TeamMessage, TeamMessageDto>()
                .ForMember(d => d.SenderUsername, o => o.MapFrom(s => s.Sender.Username))
                .ForMember(d => d.TeamName, o => o.MapFrom(s => s.Team.Name));
        }
    }

}
