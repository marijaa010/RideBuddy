import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DateFormatterService {

  /**
   * Formats a date to show relative time with full date
   * Examples: "Tomorrow at 2:00 PM", "In 3 hours", "Today at 5:30 PM"
   */
  formatRelativeDate(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = date.getTime() - now.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    // Format time
    const timeStr = date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });

    // Past dates
    if (diffMs < 0) {
      const absDays = Math.abs(diffDays);
      if (absDays === 0) {
        return `Today at ${timeStr}`;
      } else if (absDays === 1) {
        return `Yesterday at ${timeStr}`;
      } else if (absDays < 7) {
        return `${absDays} days ago`;
      } else {
        return this.formatFullDate(dateStr);
      }
    }

    // Future dates
    if (diffMins < 60) {
      return `In ${diffMins} minute${diffMins !== 1 ? 's' : ''}`;
    } else if (diffHours < 24) {
      return `In ${diffHours} hour${diffHours !== 1 ? 's' : ''} (${timeStr})`;
    } else if (diffDays === 0) {
      return `Today at ${timeStr}`;
    } else if (diffDays === 1) {
      return `Tomorrow at ${timeStr}`;
    } else if (diffDays < 7) {
      const dayName = date.toLocaleDateString('en-US', { weekday: 'long' });
      return `${dayName} at ${timeStr}`;
    } else {
      return this.formatFullDate(dateStr) + ` at ${timeStr}`;
    }
  }

  /**
   * Formats a date in full format
   * Example: "Jan 15, 2026"
   */
  formatFullDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  /**
   * Formats a date with full details
   * Example: "Jan 15, 2026 at 2:00 PM"
   */
  formatFullDateTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  }

  /**
   * Returns a countdown string for upcoming events
   * Example: "2 days, 3 hours"
   */
  getCountdown(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = date.getTime() - now.getTime();

    if (diffMs < 0) {
      return 'Started';
    }

    const days = Math.floor(diffMs / 86400000);
    const hours = Math.floor((diffMs % 86400000) / 3600000);
    const minutes = Math.floor((diffMs % 3600000) / 60000);

    if (days > 0) {
      return `${days}d ${hours}h`;
    } else if (hours > 0) {
      return `${hours}h ${minutes}m`;
    } else {
      return `${minutes}m`;
    }
  }

  /**
   * Checks if a date is in the past
   */
  isPast(dateStr: string): boolean {
    return new Date(dateStr).getTime() < new Date().getTime();
  }

  /**
   * Checks if a date is today
   */
  isToday(dateStr: string): boolean {
    const date = new Date(dateStr);
    const today = new Date();
    return date.toDateString() === today.toDateString();
  }

  /**
   * Checks if a date is tomorrow
   */
  isTomorrow(dateStr: string): boolean {
    const date = new Date(dateStr);
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    return date.toDateString() === tomorrow.toDateString();
  }
}
