using TodoApi.Models;

namespace TodoApi.Services
{
    public class LogNotifier : INotifier
    {
        private readonly ILogger<LogNotifier> _logger;

        public LogNotifier(ILogger<LogNotifier> logger)
        {
            _logger = logger;
        }

        public Task NotifyAsync(TodoItem item, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Overdue task {Id}: {Title} (due {DueDate:u}, user {UserId})",
                item.Id, item.Title, item.DueDate, item.UserId);
            return Task.CompletedTask;
        }
    }
}
