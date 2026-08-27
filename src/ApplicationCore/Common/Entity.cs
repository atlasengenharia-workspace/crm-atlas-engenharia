namespace CrmAtlas.ApplicationCore.Common;

public abstract class Entity
{
    public long Id { get; set; }
}

public interface IRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<TEntity?> FindAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
    IQueryable<TEntity> AsQueryable();
    Task<IReadOnlyList<TEntity>> ToListAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default);
    Task<int> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
