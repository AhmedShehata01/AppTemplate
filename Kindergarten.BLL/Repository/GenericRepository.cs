using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Repository
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : class
    {
        #region Prop
        private readonly ApplicationContext _context;
        private readonly DbSet<TEntity> _dbSet;
        #endregion

        #region CTOR
        public GenericRepository(ApplicationContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }
        #endregion

        #region Actions
        public async Task<IEnumerable<TEntity>> GetAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            int? page = null,
            int pageSize = 10,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            List<Expression<Func<TEntity, object>>>? includeProperties = null,
            bool noTrack = false,
            CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbSet;

            // دعم Soft Delete إن وجد
            if (typeof(BaseEntity).IsAssignableFrom(typeof(TEntity)))
            {
                query = query.Where(e => EF.Property<bool?>(e, "IsDeleted") != true);
            }

            if (filter != null)
                query = query.Where(filter);

            if (includeProperties != null)
                foreach (var includeProperty in includeProperties)
                    query = query.Include(includeProperty);

            if (noTrack)
                query = query.AsNoTracking();

            if (orderBy != null)
                query = orderBy(query);

            if (page.HasValue && page > 0)
                query = query.Skip((page.Value - 1) * pageSize).Take(pageSize);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.FindAsync(new object?[] { id }, cancellationToken);

            //if (entity is BaseEntity baseEntity)
            //    return null;

            return entity;

        }

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
                query = query.Where(filter);

            return await query.CountAsync(cancellationToken);
        }

        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.FindAsync(new object?[] { id }, cancellationToken);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task SoftDeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.FindAsync(new object?[] { id }, cancellationToken);
            if (entity != null)
            {
                if (entity is BaseEntity baseEntity)
                {
                    baseEntity.IsDeleted = true;
                    _context.Entry(entity).State = EntityState.Modified;
                }
                else
                {
                    _dbSet.Remove(entity);
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        #endregion

    }

    public interface IGenericRepository<TEntity, TKey> where TEntity : class
    {
        Task<IEnumerable<TEntity>> GetAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            int? page = null,
            int pageSize = 10,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            List<Expression<Func<TEntity, object>>>? includeProperties = null,
            bool noTrack = false,
            CancellationToken cancellationToken = default);

        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default);

        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);

        Task SoftDeleteAsync(TKey id, CancellationToken cancellationToken = default);
    }

}
