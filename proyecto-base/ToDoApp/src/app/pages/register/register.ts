import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { AuthService } from '../../services/auth-service';

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  selector: 'app-register',
  styleUrl: '../login/login.css',
  templateUrl: './register.html',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  form = this.fb.nonNullable.group({
    userName: ['', Validators.required],
    passWord: ['', Validators.required],
  });

  errorMessage = signal('');

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set('');
    const credentials = this.form.getRawValue();

    // Al registrarse se inicia sesión de una vez
    this.authService.register(credentials).pipe(
      switchMap(() => this.authService.login(credentials))
    ).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (err: HttpErrorResponse) => {
        // La API devuelve una lista con los motivos (por ejemplo, contraseña débil)
        this.errorMessage.set(Array.isArray(err.error)
          ? err.error.join(' ')
          : 'No se pudo crear la cuenta. Intente de nuevo.');
      },
    });
  }
}
