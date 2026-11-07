using TodoApi.Models;

namespace TodoApi.Services
{
    public interface INotifier
    {
        Task NotifyAsync(TodoItem item, CancellationToken cancellationToken = default);
    }
}
