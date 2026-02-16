import { Component, OnInit } from '@angular/core';
import { BookingService } from '../services/booking.service';
import { Booking } from '../domain/booking.model';

@Component({
  selector: 'app-my-bookings',
  templateUrl: './my-bookings.component.html',
  styleUrls: ['./my-bookings.component.scss']
})
export class MyBookingsComponent implements OnInit {
  bookings: Booking[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(private bookingService: BookingService) {}

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
        this.errorMessage = 'Failed to load bookings';
        this.isLoading = false;
      }
    });
  }

  cancelBooking(id: string): void {
    if (!confirm('Are you sure you want to cancel this booking?')) {
      return;
    }

    this.bookingService.cancelBooking(id).subscribe({
      next: () => {
        this.loadBookings();
      },
      error: (error) => {
        this.errorMessage = 'Failed to cancel booking';
      }
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getStatusLabel(status: number): string {
    const statuses: { [key: number]: string } = {
      0: 'Pending',
      1: 'Confirmed',
      2: 'Cancelled',
      3: 'Completed'
    };
    return statuses[status] || 'Unknown';
  }

  getStatusClass(status: number): string {
    const classes: { [key: number]: string } = {
      0: 'pending',
      1: 'confirmed',
      2: 'cancelled',
      3: 'completed'
    };
    return classes[status] || '';
  }
}
