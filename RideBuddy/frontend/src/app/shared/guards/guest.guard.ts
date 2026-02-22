import { Injectable } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Route guard that protects guest-only routes (login/register).
 * Redirects already authenticated users to main application.
 * Prevents logged-in users from accessing login/register pages.
 */
@Injectable({
  providedIn: 'root'
})
export class GuestGuard  {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  /**
   * Determines if route can be activated based on authentication status.
   * @returns true if user is NOT authenticated, otherwise redirects to /rides
   */
  canActivate(): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    if (!this.authService.isAuthenticated()) {
      return true;
    }
    
    this.router.navigate(['/rides']);
    return false;
  }
}
