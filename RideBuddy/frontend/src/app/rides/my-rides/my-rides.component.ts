import { Component, OnInit } from '@angular/core';
import { RideService } from '../services/ride.service';
import { BookingService } from '../../bookings/services/booking.service';
import { Ride } from '../domain/ride.model';
import { Booking } from '../../bookings/domain/booking.model';

@Component({
  selector: 'app-my-rides',
  templateUrl: './my-rides.component.html',
  styleUrls: ['./my-rides.component.scss']
})
export class MyRidesComponent implements OnInit {
  rides: Ride[] = [];
  rideBookings: { [rideId: string]: Booking[] } = {};
  isLoading = false;
  errorMessage = '';

  constructor(
    private rideService: RideService,
    private bookingService: BookingService
  ) {}

  ngOnInit(): void {
    this.loadMyRides();
  }

  loadMyRides(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.rideService.getMyRides().subscribe({
      next: (rides) => {
        this.rides = rides;
        this.isLoading = false;
        
        // Load bookings for each ride
        rides.forEach(ride => {
          this.loadBookingsForRide(ride.id);
        });
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Failed to load your rides';
      }
    });
  }

  loadBookingsForRide(rideId: string): void {
    this.bookingService.getBookingsByRide(rideId).subscribe({
      next: (bookings) => {
        this.rideBookings[rideId] = bookings;
      },
      error: (error) => {
        console.error('Failed to load bookings for ride', rideId);
      }
    });
  }

  confirmBooking(bookingId: string, rideId: string): void {
    if (!confirm('Confirm this booking?')) {
      return;
    }

    this.bookingService.confirmBooking(bookingId).subscribe({
      next: () => {
        // Reload bookings for this ride
        this.loadBookingsForRide(rideId);
      },
      error: (error) => {
        alert('Failed to confirm booking: ' + (error.error?.error || 'Unknown error'));
      }
    });
  }

  rejectBooking(bookingId: string, rideId: string): void {
    const reason = prompt('Enter rejection reason (optional):');
    if (reason === null) {
      return; // User cancelled
    }

    this.bookingService.rejectBooking(bookingId, reason).subscribe({
      next: () => {
        // Reload bookings for this ride
        this.loadBookingsForRide(rideId);
      },
      error: (error) => {
        alert('Failed to reject booking: ' + (error.error?.error || 'Unknown error'));
      }
    });
  }

  getBookingsForRide(rideId: string): Booking[] {
    return this.rideBookings[rideId] || [];
  }

  getPendingBookingsCount(rideId: string): number {
    return this.getBookingsForRide(rideId).filter(b => b.status === 0).length;
  }

  getConfirmedBookingsCount(rideId: string): number {
    return this.getBookingsForRide(rideId).filter(b => b.status === 1).length;
  }

  getStatusLabel(status: number): string {
    const labels: { [key: number]: string } = {
      0: 'Pending',
      1: 'Confirmed',
      2: 'Cancelled',
      3: 'Completed',
      4: 'Rejected'
    };
    return labels[status] || 'Unknown';
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

  formatDate(dateString: string): string {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric', 
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getRideStatusLabel(status: number): string {
    const labels: { [key: number]: string } = {
      0: 'Scheduled',
      1: 'In Progress',
      2: 'Completed',
      3: 'Cancelled'
    };
    return labels[status] || 'Unknown';
  }

  getRideStatusClass(status: number): string {
    const classes: { [key: number]: string } = {
      0: 'scheduled',
      1: 'in-progress',
      2: 'completed',
      3: 'cancelled'
    };
    return classes[status] || '';
  }
}
