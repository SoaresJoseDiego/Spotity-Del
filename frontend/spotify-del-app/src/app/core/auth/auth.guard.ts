import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.initialized() && auth.isAuthenticated()) return true;
  if (auth.initialized()) return router.createUrlTree(['/login']);

  return auth.initialize().pipe(
    map(() => auth.isAuthenticated() ? true : router.createUrlTree(['/login']))
  );
};
