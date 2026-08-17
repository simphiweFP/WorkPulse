import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const rolesData = route.data['roles'] as string[] | undefined;
  const singleRole = route.data['role'] as string | undefined;
  const allowed = rolesData ?? (singleRole ? [singleRole] : undefined);

  const currentUser = authService.getCurrentUserSnapshot();
  const userRole = currentUser?.role;
  const isPending = currentUser?.isPending === true || userRole === 'Pending';

  if (!allowed) {
    // No specific role required: block Pending from operational screens only when the route opts in.
    const blockPending = route.data['blockPending'] === true;
    if (blockPending && isPending) {
      return router.createUrlTree(['/access-denied']);
    }
    return true;
  }

  if (userRole && allowed.includes(userRole)) {
    return true;
  }

  return router.createUrlTree(['/access-denied']);
};
