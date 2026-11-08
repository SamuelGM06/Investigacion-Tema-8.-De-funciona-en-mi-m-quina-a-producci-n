import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import Swal from 'sweetalert2';
import { TodoItem } from '../../models/todo-item.model';
import { TodoItemService } from '../../services/todo-item-service';

@Component({
  selector: 'app-todo-list',
  imports: [RouterLink],
  templateUrl: './todo-list.html',
  styleUrl: './todo-list.css'
})
export class TodoList implements OnInit {
  private readonly todoItemService = inject(TodoItemService);

  todos = signal<TodoItem[]>([]);
  loading = signal(true);
  errorMessage = signal('');

  ngOnInit(): void {
    this.todoItemService.getAll().subscribe({
      next: (todos) => {
        this.todos.set(todos);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudieron cargar las tareas.');
        this.loading.set(false);
      },
    });
  }

  onToggle(todo: TodoItem): void {
    this.todoItemService.toggleComplete(todo.id).subscribe({
      next: (updated) =>
        this.todos.update((current) => current.map((t) => (t.id === updated.id ? updated : t))),
      error: () => this.errorMessage.set('No se pudo actualizar la tarea.'),
    });
  }

  onDelete(todo: TodoItem): void {
    Swal.fire({
      title: 'Are you sure you want to delete this task?',
      text: "You won't be able to revert this!",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText: 'Yes, delete it!',
      cancelButtonText: 'Cancel',
    }).then((result) => {
      if (!result.isConfirmed) {
        return;
      }

      this.todoItemService.delete(todo.id).subscribe({
        next: () => { this.todos.update((current) => current.filter((t) => t.id !== todo.id));

          Swal.fire({
            title: 'Deleted!',
            text: 'The task has been deleted successfully.',
            icon: 'success',
            timer: 2000,
            showConfirmButton: false,
          });
        },

        error: () => { Swal.fire({
            title: 'Error!',
            text: 'Unable to delete the task. Please try again later.',
            icon: 'error',
          });
        },
      });
    });
  }

}