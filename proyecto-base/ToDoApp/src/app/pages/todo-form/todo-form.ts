import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TodoItemService } from '../../services/todo-item-service';

@Component({
  imports: [RouterLink, ReactiveFormsModule],
  selector: 'app-todo-form',
  styleUrl: './todo-form.css',
  templateUrl: './todo-form.html',
})



export class TodoForm implements OnInit {

  private readonly todoItemService = inject(TodoItemService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);


  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.maxLength(1000)]],
  });

  errorMessage = signal('');


  editingId: number | null = null;
  private existingIsComplted = false;


  ngOnInit(): void {

    const idParam = this.route.snapshot.paramMap.get('id');

    if (!idParam)
      return;

    // si existe 
    this.editingId = Number(idParam);

    this.todoItemService.getById(this.editingId).subscribe({
      next: (todo) => {
        this.form.patchValue({
          title: todo.title,
          description: todo.description ?? '',
        });
        this.existingIsComplted = todo.isCompleted;
      },
      error: () => {
        this.errorMessage.set('Unable to load the task. Please try again later.');
      },
    });

  }




  onSubmit(): void {

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return; //early return if the form is invalid
    }

    this.errorMessage.set('');


    if (this.editingId) {
      this.todoItemService.update(this.editingId, {
        ...this.form.getRawValue(),
        isCompleted: this.existingIsComplted,

      }).subscribe({
        next: () => this.router.navigateByUrl('/tasks'),
        error: () => this.errorMessage.set('Unable to update the task. Please try again later.'),
      });
      return;
    }

    this.todoItemService.create(this.form.getRawValue()).subscribe({
      next: () => this.router.navigateByUrl('/tasks'),
      error: () => this.errorMessage.set('Unable to create the task. Please try again later.'),
    });

  }

}
