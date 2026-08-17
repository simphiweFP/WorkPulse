import { HttpErrorResponse } from '@angular/common/http';
import { AppError } from './app-error.model';

export function mapApiError(error: unknown): AppError {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as {
      title?: string;
      detail?: string;
      message?: string;
      errors?: Record<string, string[]>;
    } | null;

    switch (error.status) {
      case 0:
        return { status: 0, title: 'Connection problem', message: 'Unable to reach WorkPulse. Check your connection and try again.' };
      case 400:
        return {
          status: 400,
          title: problem?.title ?? 'Validation failed',
          message: problem?.detail ?? problem?.message ?? 'Please check the information entered and try again.',
          validationErrors: problem?.errors
        };
      case 401:
        return { status: 401, title: 'Session expired', message: 'Your session has expired. Please sign in again.' };
      case 403:
        return { status: 403, title: 'Access denied', message: problem?.detail ?? 'You do not have permission to perform this action.' };
      case 404:
        return { status: 404, title: 'Not found', message: problem?.detail ?? 'The requested item could not be found.' };
      case 409:
        return {
          status: 409,
          title: problem?.title ?? 'Conflict',
          message: problem?.detail ?? problem?.message ?? 'A conflict prevented this change.'
        };
      default:
        return { status: error.status || 500, title: problem?.title ?? 'Something went wrong', message: problem?.detail ?? problem?.message ?? 'Something went wrong. Please try again.' };
    }
  }

  return { status: 500, title: 'Something went wrong', message: 'Something went wrong. Please try again.' };
}
