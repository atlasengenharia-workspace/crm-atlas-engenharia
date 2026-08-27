using CrmAtlas.ApplicationCore.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CrmAtlas.Infrastructure.Data;

public sealed class EfRepository<TEntity>(AtlasDbContext dbContext) : IRepository<TEntity>
    where TEntity : Entity
{
    public async Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);

    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken);

    public IQueryable<TEntity> AsQueryable() =>
        dbContext.Set<TEntity>().AsNoTracking();

    public async Task<IReadOnlyList<TEntity>> ToListAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default) =>
        await query.ToListAsync(cancellationToken);

    public async Task<int> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default) =>
        await query.CountAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);

    public void Update(TEntity entity)
    {
        var entry = dbContext.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            var tracked = dbContext.Set<TEntity>().Local.FirstOrDefault(x => x.Id == entity.Id);
            if (tracked is not null)
            {
                dbContext.Entry(tracked).State = EntityState.Detached;
            }
            dbContext.Set<TEntity>().Update(entity);
        }
    }

    public void Remove(TEntity entity) => dbContext.Set<TEntity>().Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
