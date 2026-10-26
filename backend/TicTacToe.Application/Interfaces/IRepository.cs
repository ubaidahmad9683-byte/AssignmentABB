namespace TicTacToe.Application;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken) where TId : notnull;
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
