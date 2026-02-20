import { Component, OnInit, OnDestroy, HostListener, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, User } from '../../shared/services/auth.service';
import { Observable, Subscription } from 'rxjs';
import { NotificationService } from '../../shared/services/notification.service';
import { AppNotification } from '../../shared/models/notification.model';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit, OnDestroy {
  currentUser$: Observable<User | null>;
  unreadCount = 0;
  notifications: AppNotification[] = [];
  showDropdown = false;
  private subs: Subscription[] = [];

  constructor(
    public authService: AuthService,
    public notificationService: NotificationService,
    private elRef: ElementRef,
    private router: Router
  ) {
    this.currentUser$ = authService.currentUser$;
  }

  ngOnInit(): void {
    this.subs.push(
      this.notificationService.unreadCount$.subscribe((count: number) => this.unreadCount = count),
      this.notificationService.notifications$.subscribe((list: AppNotification[]) => this.notifications = list)
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
  }

  toggleNotifications(): void {
    this.showDropdown = !this.showDropdown;
    if (this.showDropdown) {
      this.notificationService.loadNotifications();
      if (this.unreadCount > 0) {
        this.notificationService.markAllAsRead();
      }
    }
  }

  onNotificationClick(notification: AppNotification): void {
    this.showDropdown = false;
    this.router.navigate(['/notifications'], { queryParams: { highlight: notification.id } });
  }

  markAllRead(): void {
    this.notificationService.markAllAsRead();
    this.showDropdown = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (this.showDropdown && !this.elRef.nativeElement.contains(event.target)) {
      this.showDropdown = false;
    }
  }

  getTimeAgo(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    return `${days}d ago`;
  }

  logout(): void {
    this.authService.logout();
  }
}