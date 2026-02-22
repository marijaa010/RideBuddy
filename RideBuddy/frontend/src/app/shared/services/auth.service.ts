import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface AuthResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  role: 'Driver' | 'Passenger';
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.loadUserFromStorage();
  }

  /**
   * Loads user and token from localStorage on app initialization.
   * If token exists, restores user session. If parsing fails, performs logout.
   */
  private loadUserFromStorage(): void {
    const token = localStorage.getItem('access_token');
    const userStr = localStorage.getItem('current_user');
    if (token && userStr) {
      try {
        const user = JSON.parse(userStr);
        this.currentUserSubject.next(user);
      } catch (e) {
        this.logout();
      }
    }
  }

  /**
   * Authenticates user with email and password.
   * On success, stores JWT token and user data in localStorage.
   * @param credentials User email and password
   * @returns Observable with authentication response including token and user data
   */
  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, credentials)
      .pipe(
        tap(response => this.setSession(response))
      );
  }

  /**
   * Registers a new user (Driver or Passenger).
   * On success, automatically logs in the user by storing token and data.
   * @param data User registration data including role selection
   * @returns Observable with authentication response
   */
  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, data)
      .pipe(
        tap(response => this.setSession(response))
      );
  }

  /**
   * Stores authentication session in localStorage and updates currentUser Observable.
   * Called after successful login or registration.
   * @param authResult Authentication response containing tokens and user data
   */
  private setSession(authResult: AuthResponse): void {
    localStorage.setItem('access_token', authResult.accessToken);
    localStorage.setItem('refresh_token', authResult.refreshToken);
    localStorage.setItem('current_user', JSON.stringify(authResult.user));
    this.currentUserSubject.next(authResult.user);
  }

  /**
   * Logs out current user by clearing all session data and redirecting to login.
   * Removes tokens from localStorage and resets currentUser Observable.
   */
  logout(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('current_user');
    this.currentUserSubject.next(null);
    this.router.navigate(['/identity/login']);
  }

  /**
   * Retrieves the JWT access token from localStorage.
   * @returns JWT token string or null if not authenticated
   */
  getToken(): string | null {
    return localStorage.getItem('access_token');
  }

  /**
   * Checks if user is currently authenticated by verifying token existence.
   * @returns true if user has valid token, false otherwise
   */
  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  /**
   * Gets the current user from the BehaviorSubject.
   * @returns Current user object or null if not authenticated
   */
  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  /**
   * Checks if current user has Driver role.
   * @returns true if user is a driver, false otherwise
   */
  isDriver(): boolean {
    const user = this.getCurrentUser();
    return user?.role === 'Driver';
  }

  /**
   * Updates current user data in both localStorage and Observable state.
   * Used when user profile is modified (e.g., name change).
   * Automatically notifies all subscribers (like Navbar) of the change.
   * @param updates Partial user object with fields to update
   */
  updateCurrentUser(updates: Partial<User>): void {
    const currentUser = this.getCurrentUser();
    if (!currentUser) return;

    const updatedUser = { ...currentUser, ...updates };
    localStorage.setItem('current_user', JSON.stringify(updatedUser));
    this.currentUserSubject.next(updatedUser);
  }
}
