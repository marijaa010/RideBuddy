import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { RideService } from '../services/ride.service';
import { Ride } from '../domain/ride.model';
import { BookingService } from '../../bookings/services/booking.service';
import { AuthService } from '../../shared/services/auth.service';
import { DateFormatterService } from '../../shared/services/date-formatter.service';
import { ToastService } from '../../shared/services/toast.service';

@Component({
  selector: 'app-ride-details',
  templateUrl: './ride-details.component.html',
  styleUrls: ['./ride-details.component.scss']
})
export class RideDetailsComponent implements OnInit {
  ride: Ride | null = null;
  isLoading = false;
  seatsToBook = 1;
  isBooking = false;

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private rideService: RideService,
    private bookingService: BookingService,
    public authService: AuthService,
    public dateFormatter: DateFormatterService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    const rideId = this.route.snapshot.paramMap.get('id');
    if (rideId) {
      this.loadRide(rideId);
    }
  }

  loadRide(id: string): void {
    this.isLoading = true;
    this.rideService.getRideById(id).subscribe({
      next: (ride) => {
        this.ride = ride;
        this.isLoading = false;
      },
      error: (error) => {
        this.toastService.error('Failed to load ride details. Please try again.');
        this.isLoading = false;
        this.router.navigate(['/rides']);
      }
    });
  }

  bookRide(): void {
    if (!this.ride || !this.authService.isAuthenticated()) {
      this.toastService.warning('Please login to book a ride.');
      this.router.navigate(['/identity/login']);
      return;
    }

    this.isBooking = true;

    this.bookingService.createBooking({
      rideId: this.ride.id,
      seatsToBook: this.seatsToBook
    }).subscribe({
      next: () => {
        this.isBooking = false;
        this.toastService.success('Booking submitted! Redirecting to your bookings...');
        setTimeout(() => {
          this.router.navigate(['/bookings']);
        }, 2000);
      },
      error: (error) => {
        this.isBooking = false;
        const errorMsg = error.error?.error || error.error?.message || 'Failed to book ride. Please try again.';
        this.toastService.error(errorMsg);
      }
    });
  }

  formatDate(dateStr: string): string {
    return this.dateFormatter.formatRelativeDate(dateStr);
  }

  getCountdown(dateStr: string): string {
    return this.dateFormatter.getCountdown(dateStr);
  }
}
