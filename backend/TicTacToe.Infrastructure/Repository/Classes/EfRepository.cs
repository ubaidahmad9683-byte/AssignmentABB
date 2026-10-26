using Microsoft.EntityFrameworkCore;
using TicTacToe.Application;
using TicTacToe.Infrastructure;

namespace TicTacToe.Infrastructure.Repository.Classes;

public sealed class EfRepository<TEntity>(GameDbContext dbContext) : IRepository<TEntity> where TEntity : class
{
    public async Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken) where TId : notnull =>
        await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken) =>
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);

    public void Update(TEntity entity) => dbContext.Set<TEntity>().Update(entity);
    public void Delete(TEntity entity) => dbContext.Set<TEntity>().Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
