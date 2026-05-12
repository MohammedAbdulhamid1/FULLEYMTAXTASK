using Eymta.core;
using Eymta.core.Interface;
using Eymta.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Eymta.Repository.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly AppDbContext _ctx;
        protected readonly DbSet<T> _set;

        public GenericRepository(AppDbContext ctx)
        {
            _ctx = ctx;
            _set = ctx.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id) =>
            await _set.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() =>
            await _set.ToListAsync();

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            await _set.Where(predicate).ToListAsync();

        public async Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate) =>
            await _set.FirstOrDefaultAsync(predicate);

        public async Task AddAsync(T entity) =>
            await _set.AddAsync(entity);

        public void Update(T entity) =>
            _set.Update(entity);

        public void Delete(T entity) =>
            _set.Remove(entity);

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) =>
            predicate is null
                ? await _set.CountAsync()
                : await _set.CountAsync(predicate);

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) =>
            await _set.AnyAsync(predicate);
    }

    // ─────────────────────────────────────────────
    //  Unit of Work
    // ─────────────────────────────────────────────
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _ctx;

        public IGenericRepository<Team> Teams { get; }
        public IGenericRepository<User> Users { get; }
        public IGenericRepository<TaskItem> Tasks { get; }
        public IGenericRepository<TaskComment> TaskComments { get; }
        public IGenericRepository<TaskAttachment> TaskAttachments { get; }
        public IGenericRepository<Notification> Notifications { get; }
        public IGenericRepository<TeamMessage> TeamMessages { get; }

        public UnitOfWork(AppDbContext ctx)
        {
            _ctx = ctx;
            Teams = new GenericRepository<Team>(ctx);
            Users = new GenericRepository<User>(ctx);
            Tasks = new GenericRepository<TaskItem>(ctx);
            TaskComments = new GenericRepository<TaskComment>(ctx);
            TaskAttachments = new GenericRepository<TaskAttachment>(ctx);
            Notifications = new GenericRepository<Notification>(ctx);
            TeamMessages = new GenericRepository<TeamMessage>(ctx);
        }

        public async Task<int> CompleteAsync() => await _ctx.SaveChangesAsync();

        public void Dispose() => _ctx.Dispose();
    }
}
