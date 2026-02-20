import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subscription, tap } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService, User } from './auth.service';
import { AppNotification } from '../models/notification.model';

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

  private async disconnect(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
    }
  }

  loadNotifications(): void {
    this.http.get<AppNotification[]>(this.apiUrl).subscribe({
      next: (notifications: AppNotification[]) => {
        this.notificationsSubject.next(notifications);
        this.unreadCountSubject.next(notifications.filter((n: AppNotification) => !n.isRead).length);
      },
      error: (err: unknown) => console.error('Failed to load notifications', err)
    });
  }

  loadUnreadCount(): void {
    this.http.get<{ count: number }>(`${this.apiUrl}/unread-count`).subscribe({
      next: (res: { count: number }) => this.unreadCountSubject.next(res.count),
      error: (err: unknown) => console.error('Failed to load unread count', err)
    });
  }

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

  markAllAsRead(): void {
    this.http.put<void>(`${this.apiUrl}/read-all`, {}).subscribe({
      next: () => {
        const updated = this.notificationsSubject.value.map((n: AppNotification) => ({ ...n, isRead: true }));
        this.notificationsSubject.next(updated);
        this.unreadCountSubject.next(0);
      }
    });
  }

  ngOnDestroy(): void {
    this.authSub.unsubscribe();
    this.disconnect();
  }
}