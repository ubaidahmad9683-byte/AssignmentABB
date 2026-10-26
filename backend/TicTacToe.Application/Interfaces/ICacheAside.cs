namespace TicTacToe.Application;

public interface ICacheAside
{
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan lifetime, CancellationToken cancellationToken);
    Task RemoveAsync(string key, CancellationToken cancellationToken);
}
