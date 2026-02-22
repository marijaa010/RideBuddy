import { Injectable } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Route guard that protects authenticated routes.
 * Redirects unauthenticated users to login page.
 * Applied to routes that require user to be logged in (e.g., /rides, /bookings, /profile).
 */
@Injectable({
  providedIn: 'root'
})
export class AuthGuard  {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  /**
   * Determines if route can be activated based on authentication status.
   * @returns true if user is authenticated, otherwise redirects to login
   */
  canActivate(): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    if (this.authService.isAuthenticated()) {
      return true;
    }
    
    this.router.navigate(['/identity/login']);
    return false;
  }
}
