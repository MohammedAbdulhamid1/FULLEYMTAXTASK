using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eymta.core.Enums
{
    public enum UserRole
    {
        Admin = 0,
        Employee = 1
    }

    public enum TaskStatusEnum
    {
        ToDo = 0,
        InProgress = 1,
        Done = 2,
        OnHold = 3
    }

    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum NotificationType
    {
        TaskAssigned = 0,
        TaskCompleted = 1,
        TaskCommented = 2,
        TaskStatusChanged = 3
    }

}
