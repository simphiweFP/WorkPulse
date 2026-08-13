import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();

  const handled = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(handled).pipe(
    catchError((error) => {
      if (error?.status === 401) {
        const currentUrl = router.url;
        if (!currentUrl.startsWith('/login') && !currentUrl.startsWith('/register')) {
          authService.logout();
          router.navigate(['/login']);
        }
      }

      return throwError(() => error);
    })
  );
};
