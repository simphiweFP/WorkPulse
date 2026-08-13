import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const requiredRole = route.data['role'] as string | undefined;
  if (!requiredRole) {
    return true;
  }

  const currentUser = authService.getCurrentUserSnapshot();
  if (currentUser?.role === requiredRole) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};
