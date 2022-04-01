namespace Caruti.Http;

public interface IWebApplication
{
    Task Listen(CancellationToken cancellationToken = default);
    void Use(Func<IRequest, IResponse, Func<Task>, Task> action);
    void Use(Func<IRequest, IResponse, Task> action);
}