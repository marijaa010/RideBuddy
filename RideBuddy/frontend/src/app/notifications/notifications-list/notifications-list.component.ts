import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NotificationService } from '../../shared/services/notification.service';
import { AppNotification, NotificationType } from '../../shared/models/notification.model';

@Component({
  selector: 'app-notifications-list',
  templateUrl: './notifications-list.component.html',
  styleUrls: ['./notifications-list.component.scss']
})
export class NotificationsListComponent implements OnInit {
  notifications: AppNotification[] = [];
  isLoading = false;
  highlightId: string | null = null;

  constructor(
    private notificationService: NotificationService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.isLoading = true;
    this.highlightId = this.route.snapshot.queryParamMap.get('highlight');

    this.notificationService.notifications$.subscribe((list: AppNotification[]) => {
      this.notifications = list;
      this.isLoading = false;

      if (this.highlightId) {
        setTimeout(() => this.scrollToHighlight(), 100);
      }
    });
    this.notificationService.loadNotifications();
  }

  private scrollToHighlight(): void {
    const el = document.getElementById('notification-' + this.highlightId);
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      setTimeout(() => { this.highlightId = null; }, 1800);
    }
  }

  markAsRead(notification: AppNotification): void {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe();
    }
  }

  markAllRead(): void {
    this.notificationService.markAllAsRead();
  }

  getTypeLabel(type: NotificationType): string {
    const labels: Record<NotificationType, string> = {
      [NotificationType.BookingCreated]: 'New Booking',
      [NotificationType.BookingConfirmed]: 'Booking Confirmed',
      [NotificationType.BookingRejected]: 'Booking Rejected',
      [NotificationType.BookingCancelled]: 'Booking Cancelled',
      [NotificationType.BookingCompleted]: 'Booking Completed'
    };
    return labels[type] ?? 'Notification';
  }

  getTypeClass(type: NotificationType): string {
    const classes: Record<NotificationType, string> = {
      [NotificationType.BookingCreated]: 'created',
      [NotificationType.BookingConfirmed]: 'confirmed',
      [NotificationType.BookingRejected]: 'rejected',
      [NotificationType.BookingCancelled]: 'cancelled',
      [NotificationType.BookingCompleted]: 'completed'
    };
    return classes[type] ?? '';
  }

  getTypeIcon(type: NotificationType): string {
    const icons: Record<NotificationType, string> = {
      [NotificationType.BookingCreated]: 'fa-calendar-plus',
      [NotificationType.BookingConfirmed]: 'fa-circle-check',
      [NotificationType.BookingRejected]: 'fa-circle-xmark',
      [NotificationType.BookingCancelled]: 'fa-ban',
      [NotificationType.BookingCompleted]: 'fa-flag-checkered'
    };
    return icons[type] ?? 'fa-bell';
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleString();
  }

  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }
}