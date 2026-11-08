import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth-service';

@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  form = this.fb.nonNullable.group({
    userName: ['', Validators.required],
    passWord: ['', Validators.required]
  });

  errorMessage = signal('');

  onSubmit(): void {

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return; //early return if the form is invalid
    }

    this.errorMessage.set('');

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => this.router.navigateByUrl('/'),

      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(typeof err.error === 'string'
          ? err.error
          : 'Unable to login. Please try again later.');
      },


    });


  }

}
