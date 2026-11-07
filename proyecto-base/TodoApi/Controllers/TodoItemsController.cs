using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoItemsController : ControllerBase
    {
        private readonly TodoDbContext _context;
        private readonly OverdueNotificationService _overdueNotificationService;

        public TodoItemsController(TodoDbContext context, OverdueNotificationService overdueNotificationService)
        {
            _context = context;
            _overdueNotificationService = overdueNotificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems(
            [FromQuery] TodoStatus? status, [FromQuery] int? categoryId, [FromQuery] bool? overdue)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = _context.TodoItems
                .Include(t => t.Category)
                .Where(t => t.UserId == userId);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            if (categoryId.HasValue)
                query = query.Where(t => t.CategoryId == categoryId.Value);

            if (overdue == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(t =>
                    t.DueDate.HasValue && t.DueDate.Value < now &&
                    t.Status != TodoStatus.Completada && t.Status != TodoStatus.Cancelada);
            }

            return Ok(await query.OrderBy(t => t.Id).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var todoItem = await _context.TodoItems
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (todoItem == null) return NotFound();
            if (todoItem.UserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this task.");

            return Ok(todoItem);
        }

        [HttpPost]
        public async Task<ActionResult<TodoItem>> CreateTodoItem(TodoItem todoItem)
        {
            if (todoItem.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == todoItem.CategoryId);
                if (!categoryExists) return BadRequest("The specified category does not exist.");
            }

            // El body trae la entidad completa, pero estos campos los decide el servidor:
            // se sobrescriben aquí, sin importar lo que haya enviado el cliente.
            todoItem.Id = 0;
            todoItem.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            todoItem.Status = TodoStatus.Pendiente;
            todoItem.CreatedAt = DateTime.UtcNow;
            todoItem.CompletedAt = null;
            todoItem.LastNotifiedDueDate = null;
            todoItem.Category = null;

            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTodoItem(int id, TodoItem updated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null) return NotFound();
            if (todoItem.UserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this task.");

            if (updated.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == updated.CategoryId);
                if (!categoryExists) return BadRequest("The specified category does not exist.");
            }

            // Solo se copian los campos que el cliente puede editar.
            // El estado no cambia aquí: para eso existe PATCH /status.
            todoItem.Title = updated.Title;
            todoItem.Description = updated.Description;
            todoItem.DueDate = updated.DueDate;
            todoItem.CategoryId = updated.CategoryId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<TodoItem>> ChangeStatus(int id, ChangeStatusRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null) return NotFound();
            if (todoItem.UserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this task.");

            try
            {
                todoItem.ChangeStatus(request.NewStatus, request.Force);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            await _context.SaveChangesAsync();
            return Ok(todoItem);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTodoItem(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null) return NotFound();
            if (todoItem.UserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this task.");

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var todoItems = await _context.TodoItems
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var now = DateTime.UtcNow;

            // Los cuatro estados aparecen siempre, aunque alguno tenga 0 tareas.
            var byStatus = Enum.GetValues<TodoStatus>()
                .ToDictionary(s => s.ToString(), s => todoItems.Count(t => t.Status == s));

            var overdue = todoItems.Count(t =>
                t.DueDate.HasValue && t.DueDate.Value < now &&
                t.Status != TodoStatus.Completada && t.Status != TodoStatus.Cancelada);

            var completed = todoItems
                .Where(t => t.Status == TodoStatus.Completada && t.CompletedAt.HasValue)
                .ToList();

            double? averageDaysToComplete = completed.Count > 0
                ? completed.Average(t => (t.CompletedAt!.Value - t.CreatedAt).TotalDays)
                : null;

            return Ok(new
            {
                total = todoItems.Count,
                byStatus,
                overdue,
                averageDaysToComplete
            });
        }

        [HttpPost("notificar-vencidas")]
        public async Task<IActionResult> NotifyOverdue()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notified = await _overdueNotificationService.NotifyOverdueAsync(userId);
            return Ok(new { notified });
        }
    }
}
