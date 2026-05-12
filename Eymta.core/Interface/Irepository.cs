using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Eymta.core.Interface
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    }

    // ─────────────────────────────────────────────
    //  Unit of Work Interface
    // ─────────────────────────────────────────────
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Team> Teams { get; }
        IGenericRepository<User> Users { get; }
        IGenericRepository<TaskItem> Tasks { get; }
        IGenericRepository<TaskComment> TaskComments { get; }
        IGenericRepository<TaskAttachment> TaskAttachments { get; }
        IGenericRepository<Notification> Notifications { get; }
        IGenericRepository<TeamMessage> TeamMessages { get; }

        Task<int> CompleteAsync();
    }
}
