using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models
{
    public class TodoItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public TodoStatus Status { get; set; } = TodoStatus.Pendiente;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public DateTime? DueDate { get; set; }

        // Fecha límite que tenía la tarea cuando se notificó por última vez.
        // Si la fecha límite cambia, deja de coincidir y la tarea se puede volver a notificar.
        public DateTime? LastNotifiedDueDate { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? UserId { get; set; }

        public void ChangeStatus(TodoStatus newStatus, bool force = false)
        {
            var validTransitions = new Dictionary<TodoStatus, TodoStatus[]>
            {
                [TodoStatus.Pendiente] = new[] { TodoStatus.EnProgreso, TodoStatus.Cancelada },
                [TodoStatus.EnProgreso] = new[] { TodoStatus.Completada, TodoStatus.Cancelada },
                [TodoStatus.Completada] = Array.Empty<TodoStatus>(),
                [TodoStatus.Cancelada] = Array.Empty<TodoStatus>()
            };

            if (!validTransitions[Status].Contains(newStatus))
                throw new InvalidOperationException($"Cannot change status from {Status} to {newStatus}.");

            var isOverdue = DueDate.HasValue && DueDate.Value < DateTime.UtcNow;
            if (newStatus == TodoStatus.Completada && isOverdue && !force)
                throw new InvalidOperationException("This task is overdue. Send force: true to complete it anyway.");

            Status = newStatus;
            if (newStatus == TodoStatus.Completada)
                CompletedAt = DateTime.UtcNow;
        }
    }
}
