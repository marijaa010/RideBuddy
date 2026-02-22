import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  duration?: number;
}

/**
 * Service for displaying toast notifications throughout the application.
 * Manages toast queue, auto-dismissal, and provides type-specific methods.
 * ToastComponent subscribes to this service to display notifications in UI.
 */
@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toasts$ = new BehaviorSubject<Toast[]>([]);
  private nextId = 0;

  /**
   * Returns Observable of current toast queue.
   * ToastComponent subscribes to this to render toasts.
   * @returns Observable stream of toast array
   */
  getToasts(): Observable<Toast[]> {
    return this.toasts$.asObservable();
  }

  /**
   * Shows a toast notification with specified type and duration.
   * Automatically dismisses after duration (if > 0).
   * @param message Notification message to display
   * @param type Toast type (success/error/warning/info) for styling
   * @param duration Auto-dismiss duration in milliseconds (0 = manual dismiss only)
   */
  show(message: string, type: Toast['type'] = 'info', duration: number = 5000): void {
    const toast: Toast = {
      id: this.nextId++,
      message,
      type,
      duration
    };

    const currentToasts = this.toasts$.value;
    this.toasts$.next([...currentToasts, toast]);

    if (duration > 0) {
      setTimeout(() => {
        this.dismiss(toast.id);
      }, duration);
    }
  }

  /**
   * Shows success toast (green, 4 seconds default).
   * Used for successful operations like "Profile updated successfully".
   * @param message Success message to display
   * @param duration Optional custom duration in milliseconds
   */
  success(message: string, duration: number = 4000): void {
    this.show(message, 'success', duration);
  }

  /**
   * Shows error toast (red, 6 seconds default).
   * Used for failed operations like "Failed to load rides".
   * @param message Error message to display
   * @param duration Optional custom duration in milliseconds
   */
  error(message: string, duration: number = 6000): void {
    this.show(message, 'error', duration);
  }

  /**
   * Shows warning toast (yellow, 5 seconds default).
   * Used for validation warnings like "Please fill in all fields".
   * @param message Warning message to display
   * @param duration Optional custom duration in milliseconds
   */
  warning(message: string, duration: number = 5000): void {
    this.show(message, 'warning', duration);
  }

  /**
   * Shows info toast (blue, 4 seconds default).
   * Used for informational messages like "No rides found".
   * @param message Info message to display
   * @param duration Optional custom duration in milliseconds
   */
  info(message: string, duration: number = 4000): void {
    this.show(message, 'info', duration);
  }

  /**
   * Dismisses a specific toast by ID.
   * Called automatically after duration expires or manually by user clicking X.
   * @param id Toast ID to dismiss
   */
  dismiss(id: number): void {
    const currentToasts = this.toasts$.value;
    this.toasts$.next(currentToasts.filter(toast => toast.id !== id));
  }

  /**
   * Clears all active toasts from the queue.
   * Rarely used; typically toasts auto-dismiss individually.
   */
  clear(): void {
    this.toasts$.next([]);
  }
}
