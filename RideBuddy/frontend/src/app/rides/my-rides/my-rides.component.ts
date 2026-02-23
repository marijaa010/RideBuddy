import { Component, OnInit } from '@angular/core';
import { RideService } from '../services/ride.service';
import { BookingService } from '../../bookings/services/booking.service';
import { Ride } from '../domain/ride.model';
import { Booking } from '../../bookings/domain/booking.model';
import { DateFormatterService } from '../../shared/services/date-formatter.service';
import { ToastService } from '../../shared/services/toast.service';
import { ModalService } from '../../shared/services/modal.service';

@Component({
  selector: 'app-my-rides',
  templateUrl: './my-rides.component.html',
  styleUrls: ['./my-rides.component.scss']
})
export class MyRidesComponent implements OnInit {
  rides: Ride[] = [];
  rideBookings: { [rideId: string]: Booking[] } = {};
  isLoading = false;

  constructor(
    private rideService: RideService,
    private bookingService: BookingService,
    public dateFormatter: DateFormatterService,
    private toastService: ToastService,
    private modalService: ModalService
  ) {}

  ngOnInit(): void {
    this.loadMyRides();
  }

  /**
   * Loads all rides created by current driver.
   * For each ride, also loads associated bookings to show pending/confirmed passengers.
   */
  loadMyRides(): void {
    this.isLoading = true;

    this.rideService.getMyRides().subscribe({
      next: (rides) => {
        this.rides = rides;
        this.isLoading = false;
        
        rides.forEach(ride => {
          this.loadBookingsForRide(ride.id);
        });
      },
      error: (error) => {
        this.isLoading = false;
        this.toastService.error('Failed to load your rides. Please try again.');
      }
    });
  }

  /**
   * Loads all bookings for a specific ride.
   * Populates rideBookings dictionary for displaying passenger list.
   * @param rideId Ride ID to fetch bookings for
   */
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

  /**
   * Confirms a pending booking request after driver approval via modal dialog.
   * Updates booking status from Pending to Confirmed.
   * @param bookingId Booking ID to confirm
   * @param rideId Ride ID (for refreshing booking list after confirmation)
   */
  confirmBooking(bookingId: string, rideId: string): void {
    this.modalService.confirm({
      title: 'Confirm Booking',
      message: 'Are you sure you want to confirm this booking request?',
      confirmText: 'Yes, Confirm',
      cancelText: 'Cancel'
    }).subscribe(confirmed => {
      if (!confirmed) return;

      this.bookingService.confirmBooking(bookingId).subscribe({
        next: () => {
          this.toastService.success('Booking confirmed successfully.');
          this.loadBookingsForRide(rideId);
        },
        error: (error) => {
          const errorMsg = error.error?.error || 'Failed to confirm booking.';
          this.toastService.error(errorMsg);
        }
      });
    });
  }

  /**
   * Rejects a pending booking request with optional reason via prompt modal.
   * Shows custom modal for entering rejection reason.
   * Releases reserved seats and notifies passenger.
   * @param bookingId Booking ID to reject
   * @param rideId Ride ID (for refreshing booking list after rejection)
   */
  rejectBooking(bookingId: string, rideId: string): void {
    this.modalService.prompt({
      title: 'Reject Booking',
      message: 'Please provide a reason for rejecting this booking:',
      confirmText: 'Reject',
      cancelText: 'Cancel',
      promptLabel: 'Rejection Reason',
      promptPlaceholder: 'e.g., Ride is full, Invalid request...',
      danger: true
    }).subscribe(reason => {
      if (reason === null) return;

      this.bookingService.rejectBooking(bookingId, reason).subscribe({
        next: () => {
          this.toastService.success('Booking rejected.');
          this.loadBookingsForRide(rideId);
        },
        error: (error) => {
          const errorMsg = error.error?.error || 'Failed to reject booking.';
          this.toastService.error(errorMsg);
        }
      });
    });
  }

  /**
   * Helper to get bookings for a specific ride from cached dictionary.
   * @param rideId Ride ID
   * @returns Array of bookings for the ride, or empty array if not loaded yet
   */
  getBookingsForRide(rideId: string): Booking[] {
    return this.rideBookings[rideId] || [];
  }

  /**
   * Counts pending bookings awaiting driver approval.
   * Used to display badge count on ride cards.
   * @param rideId Ride ID
   * @returns Number of pending bookings
   */
  getPendingBookingsCount(rideId: string): number {
    return this.getBookingsForRide(rideId).filter(b => b.status === 0).length;
  }

  /**
   * Counts confirmed bookings for a ride.
   * Used to display number of confirmed passengers.
   * @param rideId Ride ID
   * @returns Number of confirmed bookings
   */
  getConfirmedBookingsCount(rideId: string): number {
    return this.getBookingsForRide(rideId).filter(b => b.status === 1).length;
  }

  /**
   * Converts booking status enum to display label.
   * @param status Booking status number
   * @returns Status label for UI display
   */
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

  /**
   * Maps booking status to CSS class for styling status badges.
   * @param status Booking status number
   * @returns CSS class name for styling
   */
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

  /**
   * Formats date to relative format (e.g., "in 2 days", "yesterday").
   * @param dateString ISO date string
   * @returns Human-readable relative date
   */
  formatDate(dateString: string): string {
    if (!dateString) return 'N/A';
    return this.dateFormatter.formatRelativeDate(dateString);
  }

  /**
   * Converts ride status enum to display label.
   * @param status Ride status number (0=Scheduled, 1=InProgress, 2=Completed, 3=Cancelled)
   * @returns Status label for UI display
   */
  getRideStatusLabel(status: number): string {
    const labels: { [key: number]: string } = {
      0: 'Scheduled',
      1: 'In Progress',
      2: 'Completed',
      3: 'Cancelled'
    };
    return labels[status] || 'Unknown';
  }

  /**
   * Maps ride status to CSS class for styling.
   * @param status Ride status number
   * @returns CSS class name for styling
   */
  getRideStatusClass(status: number): string {
    const classes: { [key: number]: string } = {
      0: 'scheduled',
      1: 'in-progress',
      2: 'completed',
      3: 'cancelled'
    };
    return classes[status] || '';
  }

  canStartRide(ride: Ride): boolean {
    return ride.status === 0 && Date.now() >= new Date(ride.departureTime).getTime();
  }

  startRide(ride: Ride): void {
    if (!this.canStartRide(ride)) {
      this.toastService.error('Ride can be started only at or after the scheduled departure time.');
      return;
    }

    this.modalService.confirm({
      title: 'Start Ride',
      message: 'Are you sure you want to start this ride? This will mark it as In Progress.',
      confirmText: 'Yes, Start',
      cancelText: 'Cancel'
    }).subscribe(confirmed => {
      if (!confirmed) return;

      this.rideService.startRide(ride.id).subscribe({
        next: () => {
          this.toastService.success('Ride started successfully.');
          this.loadMyRides();
        },
        error: (error) => {
          const errorMsg = error.error?.error || 'Failed to start ride.';
          this.toastService.error(errorMsg);
        }
      });
    });
  }

  completeRide(rideId: string): void {
    this.modalService.confirm({
      title: 'Complete Ride',
      message: 'Are you sure you want to mark this ride as completed?',
      confirmText: 'Yes, Complete',
      cancelText: 'Cancel'
    }).subscribe(confirmed => {
      if (!confirmed) return;

      this.rideService.completeRide(rideId).subscribe({
        next: () => {
          this.toastService.success('Ride completed successfully.');
          this.loadMyRides();
        },
        error: (error) => {
          const errorMsg = error.error?.error || 'Failed to complete ride.';
          this.toastService.error(errorMsg);
        }
      });
    });
  }

  cancelRide(rideId: string): void {
    this.modalService.prompt({
      title: 'Cancel Ride',
      message: 'Are you sure you want to cancel this ride? All confirmed bookings will be cancelled.',
      confirmText: 'Cancel Ride',
      cancelText: 'Go Back',
      promptLabel: 'Cancellation Reason',
      promptPlaceholder: 'e.g., Weather conditions, personal emergency...',
      danger: true
    }).subscribe(reason => {
      if (reason === null) return;

      this.rideService.cancelRide(rideId, reason).subscribe({
        next: () => {
          this.toastService.success('Ride cancelled.');
          this.loadMyRides();
        },
        error: (error) => {
          const errorMsg = error.error?.error || 'Failed to cancel ride.';
          this.toastService.error(errorMsg);
        }
      });
    });
  }
}
