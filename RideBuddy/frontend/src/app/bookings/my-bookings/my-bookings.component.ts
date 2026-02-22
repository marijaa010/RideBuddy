import { Component, OnInit } from '@angular/core';
import { BookingService } from '../services/booking.service';
import { Booking } from '../domain/booking.model';
import { DateFormatterService } from '../../shared/services/date-formatter.service';
import { ToastService } from '../../shared/services/toast.service';
import { ModalService } from '../../shared/services/modal.service';

@Component({
  selector: 'app-my-bookings',
  templateUrl: './my-bookings.component.html',
  styleUrls: ['./my-bookings.component.scss']
})
export class MyBookingsComponent implements OnInit {
  bookings: Booking[] = [];
  isLoading = false;

  constructor(
    private bookingService: BookingService,
    public dateFormatter: DateFormatterService,
    private toastService: ToastService,
    private modalService: ModalService
  ) {}

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings(): void {
    this.isLoading = true;
    this.bookingService.getMyBookings().subscribe({
      next: (bookings) => {
        this.bookings = bookings;
        this.isLoading = false;
      },
      error: (error) => {
        this.toastService.error('Failed to load bookings. Please try again.');
        this.isLoading = false;
      }
    });
  }

  cancelBooking(id: string): void {
    this.modalService.confirm({
      title: 'Cancel Booking',
      message: 'Are you sure you want to cancel this booking? This action cannot be undone.',
      confirmText: 'Yes, Cancel',
      cancelText: 'No, Keep It',
      danger: true
    }).subscribe(confirmed => {
      if (!confirmed) return;

      this.bookingService.cancelBooking(id).subscribe({
        next: () => {
          this.toastService.success('Booking cancelled successfully.');
          this.loadBookings();
        },
        error: (error) => {
          this.toastService.error('Failed to cancel booking. Please try again.');
        }
      });
    });
  }

  formatDate(dateStr: string): string {
    return this.dateFormatter.formatRelativeDate(dateStr);
  }

  getCountdown(dateStr: string): string {
    return this.dateFormatter.getCountdown(dateStr);
  }

  getStatusLabel(status: number): string {
    const statuses: { [key: number]: string } = {
      0: 'Pending',
      1: 'Confirmed',
      2: 'Cancelled',
      3: 'Completed',
      4: 'Rejected'
    };
    return statuses[status] || 'Unknown';
  }

  getStatusClass(status: number): string {
    const classes: { [key: number]: string } = {
      0: 'pending',
      1: 'confirmed',
      2: 'cancelled',
      3: 'completed',
      4: 'rejected'
    };
    return classes[status] || '';
  }
}
