using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Services
{
    // Lógica compartida por el endpoint notificar-vencidas y por la revisión en segundo plano.
    public class OverdueNotificationService
    {
        private readonly TodoDbContext _context;
        private readonly INotifier _notifier;

        public OverdueNotificationService(TodoDbContext context, INotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        // Con userId revisa solo las tareas de ese usuario; sin userId, las de todos.
        public async Task<int> NotifyOverdueAsync(string? userId = null, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var query = _context.TodoItems.Where(t =>
                t.DueDate.HasValue && t.DueDate.Value < now &&
                t.Status != TodoStatus.Completada && t.Status != TodoStatus.Cancelada &&
                (t.LastNotifiedDueDate == null || t.LastNotifiedDueDate != t.DueDate));

            if (userId != null)
                query = query.Where(t => t.UserId == userId);

            var overdue = await query.ToListAsync(cancellationToken);

            foreach (var item in overdue)
            {
                await _notifier.NotifyAsync(item, cancellationToken);
                item.LastNotifiedDueDate = item.DueDate;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return overdue.Count;
        }
    }
}
