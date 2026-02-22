import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

/**
 * HTTP Interceptor that automatically adds JWT token to all outgoing requests.
 * Also handles 401 Unauthorized errors by logging out user.
 * Registered globally in app.module.ts.
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  /**
   * Intercepts all HTTP requests to add Authorization header with JWT token.
   * Catches 401 errors and automatically logs out user.
   * @param request Original HTTP request
   * @param next HTTP handler to pass request to
   * @returns Observable of HTTP event, with token added if available
   */
  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.authService.getToken();

    if (token) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.authService.logout();
          this.router.navigate(['/identity/login']);
        }
        return throwError(() => error);
      })
    );
  }
}
