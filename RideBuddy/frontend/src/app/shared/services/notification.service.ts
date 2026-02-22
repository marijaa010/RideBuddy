import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subscription, tap } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService, User } from './auth.service';
import { AppNotification } from '../models/notification.model';

/**
 * Service for managing real-time notifications via SignalR.
 * Automatically connects/disconnects SignalR hub based on authentication state.
 * Provides reactive streams for notifications and unread count.
 */
@Injectable({
  providedIn: 'root'
})
export class NotificationService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private notificationsSubject: BehaviorSubject<AppNotification[]>;
  private unreadCountSubject: BehaviorSubject<number>;
  private authSub: Subscription;

  notifications$: Observable<AppNotification[]>;
  unreadCount$: Observable<number>;

  private readonly apiUrl = `${environment.apiUrl}/notifications`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {
    this.notificationsSubject = new BehaviorSubject<AppNotification[]>([]);
    this.unreadCountSubject = new BehaviorSubject<number>(0);
    this.notifications$ = this.notificationsSubject as Observable<AppNotification[]>;
    this.unreadCount$ = this.unreadCountSubject as Observable<number>;

    this.authSub = this.authService.currentUser$.subscribe((user: User | null) => {
      if (user) {
        this.connect(user.id);
      } else {
        this.disconnect();
        this.notificationsSubject.next([]);
        this.unreadCountSubject.next(0);
      }
    });
  }

  /**
   * Establishes SignalR connection to Notification Hub.
   * Uses JWT token for authentication and joins user-specific group.
   * Listens for 'ReceiveNotification' events to update notification list.
   * @param userId User ID for joining SignalR group
   */
  private async connect(userId: string): Promise<void> {
    if (this.hubConnection) {
      return;
    }

    const token = this.authService.getToken();
    if (!token) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.notificationHubUrl}/hubs/notifications`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: AppNotification) => {
      const current = this.notificationsSubject.value;
      this.notificationsSubject.next([notification, ...current]);
      this.unreadCountSubject.next(this.unreadCountSubject.value + 1);
    });

    try {
      await this.hubConnection.start();
      await this.hubConnection.invoke('JoinUserGroup', userId);
      this.loadUnreadCount();
    } catch (err: unknown) {
      console.error('SignalR connection error:', err);
    }
  }

  /**
   * Stops SignalR connection and clears hub reference.
   * Called automatically when user logs out.
   */
  private async disconnect(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
    }
  }

  /**
   * Loads all notifications for current user from API.
   * Updates notifications list and recalculates unread count.
   * Called when user opens notification dropdown.
   */
  loadNotifications(): void {
    this.http.get<AppNotification[]>(this.apiUrl).subscribe({
      next: (notifications: AppNotification[]) => {
        this.notificationsSubject.next(notifications);
        this.unreadCountSubject.next(notifications.filter((n: AppNotification) => !n.isRead).length);
      },
      error: (err: unknown) => console.error('Failed to load notifications', err)
    });
  }

  /**
   * Loads unread notification count from API.
   * Called after SignalR connection is established.
   */
  loadUnreadCount(): void {
    this.http.get<{ count: number }>(`${this.apiUrl}/unread-count`).subscribe({
      next: (res: { count: number }) => this.unreadCountSubject.next(res.count),
      error: (err: unknown) => console.error('Failed to load unread count', err)
    });
  }

  /**
   * Marks a specific notification as read.
   * Updates local state and sends request to backend.
   * @param id Notification ID to mark as read
   * @returns Observable that completes when operation finishes
   */
  markAsRead(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/read`, {}).pipe(
      tap(() => {
        const updated = this.notificationsSubject.value.map((n: AppNotification) =>
          n.id === id ? { ...n, isRead: true } : n
        );
        this.notificationsSubject.next(updated);
        this.unreadCountSubject.next(Math.max(0, this.unreadCountSubject.value - 1));
      })
    );
  }

  /**
   * Marks all notifications as read.
   * Called when user opens notification dropdown.
   * Updates local state and sends request to backend.
   */
  markAllAsRead(): void {
    this.http.put<void>(`${this.apiUrl}/read-all`, {}).subscribe({
      next: () => {
        const updated = this.notificationsSubject.value.map((n: AppNotification) => ({ ...n, isRead: true }));
        this.notificationsSubject.next(updated);
        this.unreadCountSubject.next(0);
      }
    });
  }

  /**
   * Cleanup: unsubscribes from auth changes and disconnects SignalR.
   */
  ngOnDestroy(): void {
    this.authSub.unsubscribe();
    this.disconnect();
  }
}