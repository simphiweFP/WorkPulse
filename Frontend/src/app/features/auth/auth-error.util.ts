export function getAuthErrorMessage(error: unknown, fallback: string): string {
  if (!error || typeof error !== 'object') {
    return fallback;
  }

  const httpError = error as {
    status?: number;
    name?: string;
    message?: string;
    error?: { message?: string };
  };

  if (httpError.name === 'TimeoutError') {
    return 'WorkPulse is currently unavailable. Please try again.';
  }

  if (httpError.status === 0) {
    return 'WorkPulse is currently unavailable. Please try again.';
  }

  if (httpError.status === 401) {
    return 'Invalid email or password.';
  }

  if (httpError.status === 403) {
    return "You don't have permission to perform this action.";
  }

  return httpError.error?.message ?? httpError.message ?? fallback;
}
