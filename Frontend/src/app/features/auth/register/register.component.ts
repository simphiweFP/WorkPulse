import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { RegisterRequest } from '../../../core/models/auth.models';
import { getAuthErrorMessage } from '../auth-error.util';
import { FeedbackAlertService } from '../../../shared/services/feedback-alert.service';

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirmPassword = group.get('confirmPassword')?.value;

  return password === confirmPassword ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  isSubmitting = false;
  submissionFailed = false;
  errorMessage = '';
  readonly form;

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly alerts: FeedbackAlertService
  ) {
    this.form = this.fb.group(
      {
        firstName: ['', [Validators.required]],
        lastName: ['', [Validators.required]],
        email: ['', [Validators.required, Validators.email]],
        password: [
          '',
          [
            Validators.required,
            Validators.minLength(8),
            Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/)
          ]
        ],
        confirmPassword: ['', [Validators.required]]
      },
      { validators: passwordsMatchValidator }
    );
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.submissionFailed = false;
    this.errorMessage = '';

    const request = this.form.getRawValue() as RegisterRequest;
    this.authService
      .register(request)
      .pipe(
        timeout({ first: 15000 }),
        finalize(() => (this.isSubmitting = false))
      )
      .subscribe({
        next: () => {
          void this.alerts.success('Account created', 'Your account has been created successfully.').then(() => this.router.navigate(['/dashboard']));
        },
        error: (error) => {
          this.submissionFailed = true;
          this.errorMessage = getAuthErrorMessage(error, 'Registration failed.');
          void this.alerts.error('Registration failed', this.errorMessage);
        }
      });
  }

  fieldInvalid(name: 'firstName' | 'lastName' | 'email' | 'password' | 'confirmPassword'): boolean {
    const control = this.form.controls[name];
    return control.touched && control.invalid;
  }

  get passwordsMismatch(): boolean {
    return this.form.touched && !!this.form.errors?.['passwordMismatch'];
  }

  get submitLabel(): string {
    if (this.isSubmitting) {
      return 'Registering…';
    }

    return this.submissionFailed ? 'Try again' : 'Register';
  }
}
