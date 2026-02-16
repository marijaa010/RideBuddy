import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { RideService } from '../services/ride.service';
import { Ride } from '../domain/ride.model';
import { BookingService } from '../../bookings/services/booking.service';
import { AuthService } from '../../shared/services/auth.service';

@Component({
  selector: 'app-ride-details',
  templateUrl: './ride-details.component.html',
  styleUrls: ['./ride-details.component.scss']
})
export class RideDetailsComponent implements OnInit {
  ride: Ride | null = null;
  isLoading = false;
  errorMessage = '';
  seatsToBook = 1;
  isBooking = false;
  bookingSuccess = false;

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private rideService: RideService,
    private bookingService: BookingService,
    public authService: AuthService
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
        this.errorMessage = 'Failed to load ride details';
        this.isLoading = false;
      }
    });
  }

  bookRide(): void {
    if (!this.ride || !this.authService.isAuthenticated()) {
      this.router.navigate(['/identity/login']);
      return;
    }

    this.isBooking = true;
    this.errorMessage = '';

    this.bookingService.createBooking({
      rideId: this.ride.id,
      seatsToBook: this.seatsToBook
    }).subscribe({
      next: () => {
        this.bookingSuccess = true;
        this.isBooking = false;
        setTimeout(() => {
          this.router.navigate(['/bookings']);
        }, 2000);
      },
      error: (error) => {
        this.isBooking = false;
        // Backend returns error in 'error' field, not 'message'
        this.errorMessage = error.error?.error || error.error?.message || 'Failed to book ride. Please try again.';
      }
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
