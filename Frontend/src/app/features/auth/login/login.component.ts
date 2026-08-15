import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { LoginRequest } from '../../../core/models/auth.models';
import { getAuthErrorMessage } from '../auth-error.util';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  isSubmitting = false;
  submissionFailed = false;
  errorMessage = '';
  readonly form;

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.submissionFailed = false;
    this.errorMessage = '';

    const request = this.form.getRawValue() as LoginRequest;
    this.authService
      .login(request)
      .pipe(
        timeout({ first: 15000 }),
        finalize(() => (this.isSubmitting = false))
      )
      .subscribe({
        next: () => {
          this.router.navigate(['/dashboard']);
        },
        error: (error) => {
          this.submissionFailed = true;
          this.errorMessage = getAuthErrorMessage(error, 'Login failed.');
        }
      });
  }

  fieldInvalid(name: 'email' | 'password'): boolean {
    const control = this.form.controls[name];
    return control.touched && control.invalid;
  }

  get submitLabel(): string {
    if (this.isSubmitting) {
      return 'Signing in…';
    }

    return this.submissionFailed ? 'Try again' : 'Sign In';
  }
}
