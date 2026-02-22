import { Component, OnInit, OnDestroy } from '@angular/core';
import { Toast, ToastService } from '../../services/toast.service';
import { Subscription } from 'rxjs';
import { trigger, transition, style, animate } from '@angular/animations';

/**
 * Toast notification component for displaying temporary messages.
 * Subscribes to ToastService and renders toasts with slide-in animation.
 * Included globally in app.component.html for app-wide availability.
 */
@Component({
  selector: 'app-toast',
  templateUrl: './toast.component.html',
  styleUrls: ['./toast.component.scss'],
  animations: [
    trigger('toastAnimation', [
      transition(':enter', [
        style({ transform: 'translateX(100%)', opacity: 0 }),
        animate('300ms ease-out', style({ transform: 'translateX(0)', opacity: 1 }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ transform: 'translateX(100%)', opacity: 0 }))
      ])
    ])
  ]
})
export class ToastComponent implements OnInit, OnDestroy {
  toasts: Toast[] = [];
  private subscription?: Subscription;

  constructor(private toastService: ToastService) {}

  ngOnInit(): void {
    this.subscription = this.toastService.getToasts().subscribe(
      toasts => this.toasts = toasts
    );
  }

  /**
   * Cleanup: unsubscribes from ToastService to prevent memory leaks.
   */
  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  /**
   * Dismisses a toast by ID.
   * Called when user clicks X button on toast.
   * @param id Toast ID to dismiss
   */
  dismiss(id: number): void {
    this.toastService.dismiss(id);
  }

  /**
   * Maps toast type to FontAwesome icon class.
   * Used to display appropriate icon for each toast type.
   * @param type Toast type (success/error/warning/info)
   * @returns FontAwesome icon class name
   */
  getIcon(type: Toast['type']): string {
    const icons = {
      success: 'fa-circle-check',
      error: 'fa-circle-xmark',
      warning: 'fa-triangle-exclamation',
      info: 'fa-circle-info'
    };
    return icons[type];
  }
}
